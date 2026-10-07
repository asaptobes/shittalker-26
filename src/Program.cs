using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public class Phrase { public string category; public string text; public string offset; }
public class OriginalControl { public string text; public int x,y,w,h; }
public class Catalog { public List<OriginalControl> original_layout; public List<Phrase> phrases; public List<string> random; public string source_sha256; public List<List<string>> insult_groups; public List<List<string>> noise_groups; }
public static class Generators {
    public static string Generate(List<List<string>> groups, Random random, string separator) {
        if(groups==null || groups.Count!=4 || groups.Any(g=>g==null || g.Count==0))
            throw new InvalidDataException("Original generator data is missing. Restore phrases.json from this build.");
        return string.Join(separator,groups.Select(g=>g[random.Next(g.Count)]));
    }
    public static string Insult(Catalog c,Random random) {return Generate(c.insult_groups,random," ");}
    public static string SickoNoise(Catalog c,Random random) {return Generate(c.noise_groups,random,"");}
}
public class Preferences { public string[] boxes = new string[5]; public string[] buttons = new string[16]; }

sealed class OriginalVoice : IDisposable {
    Process process;
    IntPtr window;
    readonly string root;
    public OriginalVoice(string root) { this.root = root; }
    delegate bool EnumProc(IntPtr hwnd, IntPtr param);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback, IntPtr param);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
    [DllImport("user32.dll")] static extern IntPtr GetDlgItem(IntPtr hwnd, int id);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern IntPtr SendMessageTimeout(IntPtr hwnd, uint msg, IntPtr wp, string lp, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wp, IntPtr lp);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd, int command);
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hwnd);
    public async Task Ready() {
        if (process != null && !process.HasExited && IsWindow(window)) return;
        Dispose();
        var runtime = Path.Combine(root, "runtime", "otvdm-v0.9.0", "otvdmw.exe");
        var app = Path.Combine(root, "app");
        if (!File.Exists(runtime) || !File.Exists(Path.Combine(app,"DOC.EXE"))) throw new IOException("Missing original speech engine or compatibility runtime. Keep the entire ShitTalker26 folder together.");
        process = Process.Start(new ProcessStartInfo(runtime, "\"" + Path.Combine(app,"DOC.EXE") + "\"") {
            WorkingDirectory=app, UseShellExecute=false, CreateNoWindow=true, WindowStyle=ProcessWindowStyle.Hidden
        });
        for (int n=0;n<150;n++) {
            if(process.HasExited) throw new IOException("The original speech engine exited. Try restarting the voice engine.");
            uint target=(uint)process.Id;
            EnumWindows(delegate(IntPtr h, IntPtr unused) { uint pid; GetWindowThreadProcessId(h,out pid);
                if(pid==target && GetDlgItem(h,4026)!=IntPtr.Zero && GetDlgItem(h,4023)!=IntPtr.Zero) { window=h; return false; } return true;
            },IntPtr.Zero);
            if(window!=IntPtr.Zero) return;
            await Task.Delay(100);
        }
        Dispose(); throw new TimeoutException("The original speech engine did not become ready within 15 seconds.");
    }
    public async Task Speak(string text) {
        if(string.IsNullOrWhiteSpace(text)) return;
        if(text.Length>4000) throw new ArgumentException("Please keep each phrase below 4,000 characters.");
        await Ready();
        IntPtr result;
        if(SendMessageTimeout(GetDlgItem(window,4026),0x000C,IntPtr.Zero,text,2,2500,out result)==IntPtr.Zero)
            throw new IOException("The speech engine is busy or not responding. Use Stop / restart voice.");
        if(!PostMessage(GetDlgItem(window,4023),0x00F5,IntPtr.Zero,IntPtr.Zero)) throw new IOException("Could not start speech.");
    }
    public async Task ShowControls() { await Ready(); ShowWindow(window,5); SetForegroundWindow(window); }
    public void Dispose() {
        window=IntPtr.Zero;
        if(process==null) return;
        try { if(!process.HasExited) process.Kill(); } catch(InvalidOperationException) {} finally { process.Dispose(); process=null; }
    }
}

// ===================================================================================
//  Classic Win9x-style controls
// ===================================================================================

static class Classic {
    public static readonly Color Face   = Color.FromArgb(212,208,200);   // 3D face as seen in the original screenshot
    public static readonly Color Light  = Color.White;
    public static readonly Color Shadow = Color.FromArgb(128,128,128);
    public static readonly Color Dark   = Color.Black;
    public static readonly Font  UI     = new Font("Microsoft Sans Serif", 11.25f, FontStyle.Regular, GraphicsUnit.Point);

    // Win95 raised/sunken button edge (two-pixel bevel).
    public static void DrawButtonEdge(Graphics g, Rectangle r, bool pressed) {
        int l=r.Left, t=r.Top, ri=r.Right-1, b=r.Bottom-1;
        if (pressed) {
            using (var p=new Pen(Dark))   g.DrawRectangle(p, l, t, ri-l, b-t);
            using (var p=new Pen(Shadow)) g.DrawRectangle(p, l+1, t+1, ri-l-2, b-t-2);
            return;
        }
        using (var p=new Pen(Light))  { g.DrawLine(p,l,t,ri-1,t); g.DrawLine(p,l,t,l,b-1); }
        using (var p=new Pen(Dark))   { g.DrawLine(p,l,b,ri,b);   g.DrawLine(p,ri,t,ri,b); }
        using (var p=new Pen(Shadow)) { g.DrawLine(p,l+1,b-1,ri-1,b-1); g.DrawLine(p,ri-1,t+1,ri-1,b-1); }
    }

    // Etched group frame with a caption, like a VB3 Frame control.
    public static void DrawFrame(Graphics g, Rectangle r, string caption, Font font) {
        int half = font.Height/2;
        var box = new Rectangle(r.X, r.Y+half, r.Width-1, r.Height-half-1);
        using (var p=new Pen(Shadow)) g.DrawRectangle(p, box.X, box.Y, box.Width-1, box.Height-1);
        using (var p=new Pen(Light))  g.DrawRectangle(p, box.X+1, box.Y+1, box.Width-1, box.Height-1);
        if (string.IsNullOrEmpty(caption)) return;
        var size = TextRenderer.MeasureText(g, caption, font, Size.Empty, TextFormatFlags.NoPadding);
        var cap = new Rectangle(r.X+8, r.Y, size.Width+4, size.Height);
        using (var bg=new SolidBrush(Face)) g.FillRectangle(bg, cap);
        TextRenderer.DrawText(g, caption, font, cap, Color.Black, TextFormatFlags.HorizontalCenter|TextFormatFlags.Top|TextFormatFlags.NoPadding);
    }
}

sealed class RetroButton : Button {
    bool pressed, mouseInside, keyDown;
    public RetroButton() {
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw, true);
        BackColor = Classic.Face; ForeColor = Color.Black; Font = Classic.UI; UseVisualStyleBackColor = false;
    }
    protected override void OnMouseDown(MouseEventArgs e) { if (e.Button==MouseButtons.Left) { pressed=true; mouseInside=true; Invalidate(); } base.OnMouseDown(e); }
    protected override void OnMouseUp(MouseEventArgs e)   { if (pressed) { pressed=false; Invalidate(); } base.OnMouseUp(e); }
    protected override void OnMouseMove(MouseEventArgs e) {
        if (pressed) { bool inside=ClientRectangle.Contains(e.Location); if (inside!=mouseInside) { mouseInside=inside; Invalidate(); } }
        base.OnMouseMove(e);
    }
    protected override void OnKeyDown(KeyEventArgs e) { if (e.KeyCode==Keys.Space) { keyDown=true; Invalidate(); } base.OnKeyDown(e); }
    protected override void OnKeyUp(KeyEventArgs e)   { if (keyDown) { keyDown=false; Invalidate(); } base.OnKeyUp(e); }
    protected override void OnLostFocus(EventArgs e)  { keyDown=false; Invalidate(); base.OnLostFocus(e); }
    protected override void OnGotFocus(EventArgs e)   { Invalidate(); base.OnGotFocus(e); }

    protected override void OnPaint(PaintEventArgs e) {
        var g = e.Graphics; var r = ClientRectangle;
        bool down = (pressed && mouseInside) || keyDown;
        using (var bg=new SolidBrush(Classic.Face)) g.FillRectangle(bg, r);
        Classic.DrawButtonEdge(g, r, down);
        var textRect = new Rectangle(r.X+3, r.Y+1, r.Width-6, r.Height-2);
        if (down) textRect.Offset(1,1);
        TextRenderer.DrawText(g, Text, Font, textRect, Enabled?ForeColor:Classic.Shadow,
            TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPrefix);
        if (Focused) {
            var f = new Rectangle(r.X+3, r.Y+3, r.Width-7, r.Height-7);
            if (down) f.Offset(1,1);
            ControlPaint.DrawFocusRectangle(g, f, Color.Black, Classic.Face);
        }
    }
}

// A black, borderless entry field like the originals (TextBox can't size its own height freely).
sealed class BlackField : Panel {
    public readonly TextBox Box = new TextBox { BorderStyle=BorderStyle.None, BackColor=Color.Black, ForeColor=Color.White, Font=Classic.UI, MaxLength=200 };
    public BlackField() {
        BackColor = Color.Black; Controls.Add(Box);
        Resize += (s,e) => Box.SetBounds(3, Math.Max(0,(Height-Box.PreferredHeight)/2), Math.Max(10,Width-6), Box.PreferredHeight);
        Click += (s,e) => Box.Focus();
    }
    public override string Text { get { return Box.Text; } set { Box.Text = value; } }
}

// ===================================================================================
//  Main window — laid out to match the Shit talker 1.2 screen
// ===================================================================================

sealed class MainForm : Form {
    readonly string root=AppDomain.CurrentDomain.BaseDirectory;
    readonly JavaScriptSerializer json=new JavaScriptSerializer();
    readonly OriginalVoice voice;
    readonly List<TextBox> quickBoxes=new List<TextBox>();
    readonly Random random=new Random();
    readonly List<KeyValuePair<Rectangle,string>> frames=new List<KeyValuePair<Rectangle,string>>();
    readonly BlackField nameField=new BlackField();
    readonly BlackField callerField=new BlackField();
    readonly Catalog catalog;
    readonly Preferences prefs;
    readonly string settings;
    CustomButtonsForm customForm;
    bool busy, closing;

    const int VisibleQuickBoxes = 3;          // the original shows three at 640x480
    const int OX = 4, OY = 31;                // offsets from the reference screenshot to client area

    // Reference-screenshot coordinates -> client rectangle.
    static Rectangle R(int x1,int y1,int x2,int y2) { return new Rectangle(x1-OX, y1-OY, x2-x1, y2-y1); }

    public MainForm() {
        Text = "Shit Talker 2026  by: asaptobes  -  based on the original Shit Talker 1.2: by jaundice      \u00AF`\u00B7.\u00B8\u00B8.\u00B7\u00B4\u00AF) http://members.aol.com/meatsloth (\u00AF`\u00B7.\u00B8\u00B8.\u00B7\u00B4\u00AF";
        AutoScaleDimensions = new SizeF(96,96); AutoScaleMode = AutoScaleMode.Dpi;
        Font = Classic.UI; BackColor = Classic.Face;
        ClientSize = new Size(1016, 728);
        FormBorderStyle = FormBorderStyle.FixedSingle; MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        DoubleBuffered = true;
        KeyPreview = true;

        voice = new OriginalVoice(root);
        settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"ShitTalker26","settings.json");
        catalog = json.Deserialize<Catalog>(File.ReadAllText(Path.Combine(root,"phrases.json")));
        prefs = LoadPreferences();

        BuildMenu();
        BuildTopStrip();
        BuildQuestionGrid();
        BuildResponseGrid();
        BuildThisColumn();
        BuildQuestionList();
        BuildStatementList();
        BuildQuickBoxes();
        Shown += (s,e) => nameField.Box.Focus();

        FormClosing += (s,e) => {
            try { SavePreferences(); }
            catch (Exception ex) {
                if (MessageBox.Show(this,"Your settings could not be saved: "+ex.Message+"\nClose anyway?","Save settings",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.No) { e.Cancel=true; return; }
            }
            closing = true; voice.Dispose();
        };
    }

    // ---------------------------------------------------------------- layout sections

    void BuildMenu() {
        var menu = new MenuStrip { Dock=DockStyle.None, BackColor=Classic.Face, Font=Classic.UI, Renderer=new ToolStripSystemRenderer(),
                                   GripStyle=ToolStripGripStyle.Hidden, Padding=new Padding(0), AutoSize=false };
        menu.SetBounds(0, 0, ClientSize.Width, 28);

        var about = new ToolStripMenuItem("| About");
        about.Click += (s,e) => MessageBox.Show(this,
            "Shit Talker 2026 by: asaptobes\n\nBased on the original Shit Talker 1.2: by jaundice\nhttp://members.aol.com/meatsloth\n\nOriginal SoftVoice / Willow Pond DocTalker speech engine,\nrunning on Windows 11 through WineVDM.","About Shit Talker 2026");
        var options = new ToolStripMenuItem("| Options |");
        options.DropDownItems.Add("Voice settings...", null, async (s,e) => await Run(() => voice.ShowControls()));
        options.DropDownItems.Add("Custom buttons...", null, (s,e) => ShowCustomButtons());
        options.DropDownItems.Add(new ToolStripSeparator());
        options.DropDownItems.Add("Stop / restart voice", null, async (s,e) => await RestartVoice());
        var restart = new ToolStripMenuItem("| Restart DocTalker |");
        restart.Click += async (s,e) => await RestartVoice();
        var quit = new ToolStripMenuItem("| Quit Shit-Talker |");
        quit.Click += (s,e) => Close();

        foreach (var item in new[]{about,options,restart,quit}) { item.Padding = new Padding(4,0,4,0); menu.Items.Add(item); }
        Controls.Add(menu); MainMenuStrip = menu;
    }

    void BuildTopStrip() {
        Func<string> askFor = () => "Hi, can I talk to " + nameField.Text.Trim() + " please?";
        AddButton("Hi, can I talk to", R(16,64,165,90),  () => Say(askFor()));
        nameField.Bounds = R(167,64,265,90); nameField.Box.AccessibleName = "Name to ask for"; Controls.Add(nameField);
        AddButton("please?",           R(267,64,341,90), () => Say(askFor()));

        AddButton("This is/",          R(357,64,431,90), () => Say("This is " + callerField.Text.Trim()));
        AddButton("I'm",               R(433,64,466,90), () => Say("I'm " + callerField.Text.Trim()));
        callerField.Bounds = R(468,64,620,90); callerField.Box.AccessibleName = "Your name"; Controls.Add(callerField);

        AddButton("Jam radar!",        R(634,64,822,90), () => Say(catalog.random[random.Next(catalog.random.Count)]));
        AddButton("Exit",              R(836,64,999,90), () => { Close(); return Task.FromResult(0); });

        nameField.Box.KeyDown   += async (s,e) => { if (e.KeyCode==Keys.Enter) { e.SuppressKeyPress=true; await Say(askFor()); } };
        callerField.Box.KeyDown += async (s,e) => { if (e.KeyCode==Keys.Enter) { e.SuppressKeyPress=true; await Say("This is " + callerField.Text.Trim()); } };
    }

    void BuildQuestionGrid() {
        AddFrame("Questions:", R(15,103,342,238));
        string[,] q = { {"Who?","Where?","When?","Well?"}, {"What?","Why?","How?","Now?"} };
        int[] xs = {28,104,180,256};
        int[] ys = {127,178};
        int[] hs = {49,48};
        for (int row=0; row<2; row++)
            for (int col=0; col<4; col++)
                AddPhrase(q[row,col], R(xs[col], ys[row], xs[col]+74, ys[row]+hs[row]));
    }

    void BuildResponseGrid() {
        AddFrame("Statements/responses:", R(356,103,819,238));
        int[] ys = {127,152,177,202};
        AddPhrase("Yes",          R(369,ys[0],443,ys[0]+23));
        AddPhrase("No",           R(445,ys[0],517,ys[0]+23));
        AddPhrase("I don't know", R(520,ys[0],656,ys[0]+23));
        AddPhrase("Hello",        R(659,ys[0],809,ys[0]+23));
        string[,] rows = { {"OK","I think so.","Goodbye"}, {"Maybe","That is nice.","Thank you"}, {"I will.","Wow, cool.","You're welcome"} };
        int[] x1 = {369,520,659}, x2 = {517,656,809};
        for (int r=0; r<3; r++)
            for (int c=0; c<3; c++)
                AddPhrase(rows[r,c], R(x1[c], ys[r+1], x2[c], ys[r+1]+23));
    }

    void BuildThisColumn() {
        AddFrame("This:", R(836,103,998,305));
        string[] items = {"Fuck you!","Shut up.","Asshole","Eat my shit.","Go to hell.","Monkey face","So what?"};
        for (int i=0; i<items.Length; i++) {
            int y = 126 + (int)Math.Round(i*25.2);
            AddPhrase(items[i], R(848,y,986,y+23));
        }
        AddButton("Random Insult", R(848,315,986,352), () => Say(Generators.Insult(catalog,random)));
        AddButton("Sicko noise",   R(848,355,986,392), () => Say(Generators.SickoNoise(catalog,random)));
    }

    static int ListY(int i) { return 266 + (int)Math.Round(i*25.25); }

    void BuildQuestionList() {
        AddFrame("", R(15,248,342,755));
        string[] questions = {"Hello, who is this?","Hello, are you still there?","Do you know what time it is?","Do you have any dogs?",
            "Do you have any children?","Do you have bikes?","Don't you remember me?","Do you dance?","Can I come over?",
            "Can I talk to someone else?","Are you busy tonight?","Can we meet?","Can I ask you a question?",
            "About how long does that take?","Will you be there?","Can i speak with a manager?","What time are you open until?",
            "Is that OK?","Is *random name* there?"};
        for (int i=0; i<questions.Length; i++) AddPhrase(questions[i], R(28,ListY(i),330,ListY(i)+23));
    }

    void BuildStatementList() {
        AddFrame("", R(356,248,822,755));
        string[] left  = {"I just want to talk.","I am confused","I will be over soon.","I need to talk to you.","i have a cold","I have about 6 myself.","Why not?"};
        string[] right = {"Please calm down.","Please do not shout.","Please don't do that.","Please say something.","My car is broken.","What did you call me?","What did you say?"};
        for (int i=0; i<7; i++) {
            AddPhrase(left[i],  R(369,ListY(i),582,ListY(i)+23));
            AddPhrase(right[i], R(596,ListY(i),809,ListY(i)+23));
        }
        string[] full = {"I have another question.","Like to do something this weekend?","Oh it is just a figure of speech.",
            "I was wondering if you could help me.","I will call back tomorrow.","I'm returning your phone call.","Haa ha haaaa. that is funny",
            "Robots are very strong.","I'm selling robot insurance.","Send me some parts.","There is a reason for all of this.","I'm not joking around anymore."};
        for (int i=0; i<full.Length; i++) AddPhrase(full[i], R(369,ListY(7+i),809,ListY(7+i)+23));
    }

    void BuildQuickBoxes() {
        AddFrame("Quick Boxes:", R(836,420,998,755));
        int[] boxTop = {440,542,643};
        for (int i=0; i<VisibleQuickBoxes; i++) {
            var box = new TextBox { Multiline=true, ScrollBars=ScrollBars.Vertical, WordWrap=true, MaxLength=4000,
                                    BorderStyle=BorderStyle.Fixed3D, BackColor=Color.Black, ForeColor=Color.White, Font=Classic.UI,
                                    Text=prefs.boxes[i] ?? "", AccessibleName="Quick box "+(i+1) };
            box.Bounds = R(848, boxTop[i], 986, boxTop[i]+77);
            Controls.Add(box); quickBoxes.Add(box);
            var captured = box;
            box.KeyDown += async (s,e) => { if (e.Control && e.KeyCode==Keys.Enter) { e.SuppressKeyPress=true; await Say(captured.Text); } };
            AddButton("Say it...", R(848, boxTop[i]+78, 986, boxTop[i]+100), () => Say(captured.Text));
        }
    }

    // ---------------------------------------------------------------- painting

    protected override void OnPaint(PaintEventArgs e) {
        base.OnPaint(e);
        foreach (var f in frames) Classic.DrawFrame(e.Graphics, f.Key, f.Value, Font);
    }

    // ---------------------------------------------------------------- helpers

    void AddFrame(string caption, Rectangle r) { frames.Add(new KeyValuePair<Rectangle,string>(r, caption)); }

    void AddPhrase(string text, Rectangle r) { string phrase = text; AddButton(text, r, () => Say(phrase)); }

    RetroButton AddButton(string text, Rectangle r, Func<Task> action) {
        var b = new RetroButton { Text=text, Bounds=r };
        b.Click += async (s,e) => {
            try { await action(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Shit-Talker", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        };
        Controls.Add(b);
        return b;
    }

    void ShowCustomButtons() {
        if (customForm == null || customForm.IsDisposed) customForm = new CustomButtonsForm(prefs, Say);
        if (!customForm.Visible) customForm.Show(this);
        customForm.Activate();
    }

    async Task RestartVoice() {
        if (busy) return;
        voice.Dispose();
        await Run(() => voice.Ready());
    }

    async Task Run(Func<Task> action) {
        if (busy || closing) return;
        busy = true;
        try { await action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message + "\n\nTry Restart DocTalker.", "DocTalker", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { busy = false; if (closing) voice.Dispose(); }
    }

    public Task Say(string text) { return Run(() => voice.Speak(text)); }

    // ---------------------------------------------------------------- settings

    Preferences LoadPreferences() {
        if (File.Exists(settings)) {
            try {
                var p = json.Deserialize<Preferences>(File.ReadAllText(settings));
                if (p!=null && p.boxes!=null && p.boxes.Length==5 && p.buttons!=null && p.buttons.Length==16) return p;
            } catch {}
        }
        var result = new Preferences();
        var ini = Path.Combine(root,"app","prog.ini");
        if (!File.Exists(ini)) return result;
        string section = "";
        foreach (var line in File.ReadAllLines(ini)) {
            if (line.StartsWith("[")) section = line.Trim('[',']');
            else if (line.StartsWith("text=")) {
                int n;
                if (section.StartsWith("text") && int.TryParse(section.Substring(4),out n) && n>=1 && n<=5) result.boxes[n-1] = line.Substring(5);
                if (section.StartsWith("but")  && int.TryParse(section.Substring(3),out n) && n>=1 && n<=16) result.buttons[n-1] = line.Substring(5);
            }
        }
        return result;
    }

    void SavePreferences() {
        // Only the visible boxes are editable here; boxes 4-5 keep whatever was saved before.
        for (int i=0; i<quickBoxes.Count; i++) prefs.boxes[i] = quickBoxes[i].Text;
        Directory.CreateDirectory(Path.GetDirectoryName(settings));
        var tmp = settings + ".tmp";
        File.WriteAllText(tmp, json.Serialize(prefs), Encoding.UTF8);
        if (File.Exists(settings)) File.Replace(tmp, settings, settings + ".bak"); else File.Move(tmp, settings);
    }
}

// ===================================================================================
//  The 16 customizable buttons (v1.2 feature) live in their own classic tool window
// ===================================================================================

sealed class CustomButtonsForm : Form {
    readonly Preferences prefs;
    readonly Func<string,Task> say;
    readonly RetroButton[] buttons = new RetroButton[16];

    public CustomButtonsForm(Preferences prefs, Func<string,Task> say) {
        this.prefs = prefs; this.say = say;
        Text = "Custom buttons  -  right-click to edit";
        AutoScaleDimensions = new SizeF(96,96); AutoScaleMode = AutoScaleMode.Dpi;
        Font = Classic.UI; BackColor = Classic.Face;
        FormBorderStyle = FormBorderStyle.FixedToolWindow; ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(4*170+24, 4*30+24);
        for (int i=0; i<16; i++) {
            int index = i;
            var b = new RetroButton { Bounds = new Rectangle(12+(i%4)*170, 12+(i/4)*30, 166, 26) };
            b.MouseUp += (s,e) => { if (e.Button==MouseButtons.Right) Edit(index); };
            b.Click += async (s,e) => {
                if (string.IsNullOrWhiteSpace(prefs.buttons[index])) Edit(index);
                else await say(prefs.buttons[index]);
            };
            buttons[i] = b; Controls.Add(b); Refresh(index);
        }
    }

    void Refresh(int i) {
        var t = prefs.buttons[i];
        buttons[i].Text = string.IsNullOrWhiteSpace(t) ? "Button " + (i+1) : t.Replace("\r"," ").Replace("\n"," ");
    }

    void Edit(int i) {
        using (var d = new Form { Text="Custom button "+(i+1), ClientSize=new Size(460,190), StartPosition=FormStartPosition.CenterParent,
                                  MinimizeBox=false, MaximizeBox=false, FormBorderStyle=FormBorderStyle.FixedDialog, BackColor=Classic.Face, Font=Classic.UI }) {
            var t = new TextBox { Multiline=true, ScrollBars=ScrollBars.Vertical, Text=prefs.buttons[i] ?? "", MaxLength=4000,
                                  Bounds=new Rectangle(10,10,440,130), BackColor=Color.Black, ForeColor=Color.White };
            var ok = new RetroButton { Text="OK", Bounds=new Rectangle(270,150,85,28), DialogResult=DialogResult.OK };
            var cancel = new RetroButton { Text="Cancel", Bounds=new Rectangle(365,150,85,28), DialogResult=DialogResult.Cancel };
            d.Controls.AddRange(new Control[]{t,ok,cancel}); d.CancelButton = cancel;
            if (d.ShowDialog(this) == DialogResult.OK) { prefs.buttons[i] = t.Text; Refresh(i); }
        }
    }
}

static class Program {
    [STAThread] static void Main() {
        Application.SetCompatibleTextRenderingDefault(false);
        try { Application.Run(new MainForm()); }
        catch (Exception ex) { MessageBox.Show(ex.ToString(), "ShitTalker could not start"); }
    }
}
