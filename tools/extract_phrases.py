"""Recover text from the supplied, hash-identified ShitTalker 1.2 binary."""
import hashlib
import json
import pathlib
import re
import struct

root = pathlib.Path(__file__).resolve().parents[1]
data = (root / 'original-extracted/Talker.exe').read_bytes()
strings = [(m.start(), m.group().decode('ascii')) for m in re.finditer(rb'[ -~]{5,}', data)]
phrases = []
for offset, text in strings:
    if 0x135ee <= offset <= 0x147bc:
        if text in ['Questions:', 'Statements/responses:', 'This:']:
            continue
        if text[0] in '\"!%' and text[1:2].isalpha():
            text = text[1:]
        category = 'Responses' if offset < 0x13fff else 'Questions' if offset < 0x144f2 else 'Miscellaneous'
        phrases.append(dict(category=category, text=text, offset=hex(offset)))
random_lines = []
for offset, text in strings:
    if text == 'I have legs, twice.':
        start = offset
    if 'start' in locals() and start <= offset and offset < 0x4000:
        if text.startswith('7]SKI') or text.startswith('KI') or not re.search('[a-z]{3}', text):
            continue
        if text in ['Define Custom Voices', 'your mom is a']:
            break
        if ' ' in text and len(text) > 12:
            random_lines.append(text)
payload = {'phrases': phrases, 'random': list(dict.fromkeys(random_lines)),
           'source_sha256': hashlib.sha256(data).hexdigest()}

def literals(start, end):
    # VB3 counted string literal: 9a38, record size, relative address, length, bytes.
    result = []
    for match in re.finditer(rb'\x9a\x38', data[start:end]):
        offset = start + match.start()
        length = struct.unpack_from('<H', data, offset + 6)[0]
        value = data[offset + 8:offset + 8 + length]
        if not value or offset + 8 + length > end or any(c < 32 or c > 126 for c in value):
            raise ValueError(f'Unexpected VB3 literal at {offset:x}')
        result.append(value.decode('ascii'))
    return result

payload['insult_groups'] = [literals(a,b) for a,b in
    [(0x3200,0x332c),(0x332c,0x3d4c),(0x3d4c,0x493a),(0x493a,0x5592)]]
payload['noise_groups'] = [literals(a,b) for a,b in
    [(0x55fa,0x570a),(0x570a,0x581c),(0x581c,0x591c),(0x591c,0x5a30)]]
assert [len(g) for g in payload['noise_groups']] == [6,6,6,6]
layout = []
for m in re.finditer(rb'[ -~]{4,}', data[0x13300:0x14850]):
    offset, end = 0x13300+m.start(), 0x13300+m.end()
    if data[end:end+1] != b'\x04':
        continue
    text = m.group().decode('ascii')
    if text[0] in '\"!%' and text[1:2].isalpha():
        text = text[1:]
    x,y,w,h = [round(n/15) for n in struct.unpack_from('<4H',data,end+1)]
    if y >= 24 and offset < 0x14799:
        layout.append(dict(text=text,x=x,y=y,w=w,h=h))
payload['original_layout'] = layout
print('Insult pool sizes:', [len(g) for g in payload['insult_groups']])
destination = root / 'ShitTalker26/phrases.json'
destination.write_text(json.dumps(payload, indent=2), encoding='utf-8')
print(f'Recovered {len(phrases)} buttons and {len(payload["random"])} random candidates')
