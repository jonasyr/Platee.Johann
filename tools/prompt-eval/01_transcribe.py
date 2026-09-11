"""Transkribiert die Testaufnahmen EINMAL und legt die Transkripte ab.

Die Transkription ist der einzige Teil, der Audio kostet. Danach laeuft jede
Prompt-Runde nur noch auf Text. Vorhandene Transkripte werden nicht neu erzeugt.
"""
import io, json, os, sys, mimetypes, urllib.request, uuid

AUDIO_DIR = os.path.join(os.environ['USERPROFILE'], 'Documents', 'Johann Test Aufnahmen')
OUT = os.path.join(os.environ['USERPROFILE'], 'Documents', 'Johann', 'prompt-sandbox',
                   'eval', 'corpus_recordings.json')
MODEL = 'gpt-transcribe'


def api_key():
    env = os.path.join(os.environ['USERPROFILE'], 'Documents', 'Johann', '.env')
    for line in io.open(env, encoding='utf-8-sig'):
        if line.strip().startswith('OPENAI_API_KEY='):
            return line.split('=', 1)[1].strip().strip('"\'')
    raise SystemExit('kein OPENAI_API_KEY gefunden')


def transcribe(path, key):
    boundary = uuid.uuid4().hex
    name = os.path.basename(path)
    ctype = mimetypes.guess_type(name)[0] or 'audio/mpeg'
    body = bytearray()

    def field(k, v):
        body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'
                    .encode('utf-8'))

    field('model', MODEL)
    body.extend(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; '
                f'filename="{name}"\r\nContent-Type: {ctype}\r\n\r\n'.encode('utf-8'))
    body.extend(io.open(path, 'rb').read())
    body.extend(f'\r\n--{boundary}--\r\n'.encode('utf-8'))

    req = urllib.request.Request(
        'https://api.openai.com/v1/audio/transcriptions', data=bytes(body),
        headers={'Authorization': f'Bearer {key}',
                 'Content-Type': f'multipart/form-data; boundary={boundary}'})
    return json.load(urllib.request.urlopen(req, timeout=900))['text']


def main():
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    done = {}
    if os.path.exists(OUT):
        done = {e['id']: e for e in json.load(io.open(OUT, encoding='utf-8'))}
        print(f'{len(done)} Transkripte bereits vorhanden')

    key = api_key()
    files = sorted(f for f in os.listdir(AUDIO_DIR) if f.lower().endswith('.mp3'))
    for i, f in enumerate(files, 1):
        if f in done:
            print(f'  [{i}/{len(files)}] {f}: uebersprungen')
            continue
        path = os.path.join(AUDIO_DIR, f)
        mb = os.path.getsize(path) / 1e6
        print(f'  [{i}/{len(files)}] {f} ({mb:.1f} MB) …', end='', flush=True)
        try:
            text = transcribe(path, key)
            done[f] = {'id': f, 'source': 'aufnahme', 'chars': len(text), 'text': text}
            print(f' {len(text)} Zeichen')
        except Exception as e:
            print(f' FEHLER: {str(e)[:120]}')
        json.dump(list(done.values()), io.open(OUT, 'w', encoding='utf-8'),
                  ensure_ascii=False, indent=1)

    print(f'\n{len(done)} Transkripte in {OUT}')


if __name__ == '__main__':
    main()
