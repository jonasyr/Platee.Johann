/* SettingsDialog.jsx — modal "Einstellungen" with grouped section list + per-section editor */

const SECTIONS = [
  { group: 'Grunddaten',           id: 'general',  label: 'Allgemein' },
  { group: 'Grunddaten',           id: 'paths',    label: 'Verzeichnisse' },
  { group: 'Globale Prompts',      id: 'system',   label: 'System-Nachricht' },
  { group: 'Globale Prompts',      id: 'abstract', label: 'Kurzfassung' },
  { group: 'Globale Prompts',      id: 'struct',   label: 'Zusammenfassung' },
  { group: 'Globale Prompts',      id: 'prose',    label: 'Ausführlich' },
  { group: 'Typ-spezifische Prompts', id: 'email',     label: 'E-Mail' },
  { group: 'Typ-spezifische Prompts', id: 'aufgabe',   label: 'Aufgaben' },
  { group: 'Typ-spezifische Prompts', id: 'gespraech', label: 'Gesprächsnotiz' },
  { group: 'Typ-spezifische Prompts', id: 'stunden',   label: 'Stundenzettel' },
  { group: 'Typ-spezifische Prompts', id: 'analog',    label: 'Analog' },
];

const DEFAULT_PROMPT = `Du bist Johann, ein präziser Diktat-Assistent. Fasse das folgende Transkript zusammen:

- Strukturierte Gliederung in Markdown
- Maximal {word_limit} Wörter
- Behalte Fachbegriffe bei
- Schreibe in der dritten Person

Transkript:
{transcript}`;

const SettingsDialog = ({ open, onClose, findings = {}, onSaved }) => {
  const [section, setSection] = React.useState('general');
  const [name, setName] = React.useState('Jonas Y.');
  const [firma, setFirma] = React.useState('Peano');
  const [paths, setPaths] = React.useState({
    quell: 'C:\\Users\\Jonas\\Dokumente\\Johann\\Eingang',
    archiv: 'C:\\Users\\Jonas\\Dokumente\\Johann\\Eingang\\Archiv',
    ausgabe: 'C:\\Users\\Jonas\\Dokumente\\Johann\\output',
  });
  const [saved, setSaved] = React.useState('');

  if (!open) return null;

  const grouped = SECTIONS.reduce((acc, s) => {
    (acc[s.group] = acc[s.group] || []).push(s);
    return acc;
  }, {});

  return (
    <div className="pj-modal-scrim" onClick={onClose}>
      <div className="pj-window pj-settings" onClick={e => e.stopPropagation()}>
        <div className="pj-window-chrome">
          <img src="../assets/Johann_256.png" className="pj-window-chrome-icon" alt="" data-pjicon="" />
          <span className="pj-window-title">Einstellungen – Platé.Johann</span>
          <div className="pj-window-chrome-controls">
            <button className="pj-tb-btn" title="Minimieren">−</button>
            <button className="pj-tb-btn" title="Maximieren">□</button>
            <button className="pj-tb-btn pj-tb-close" onClick={onClose} title="Schließen">✕</button>
          </div>
        </div>

        <div className="pj-settings-body">
          <aside className="pj-settings-nav">
            <div className="pj-settings-nav-title">Bereiche</div>
            {Object.entries(grouped).map(([g, items]) => (
              <div key={g} className="pj-settings-group">
                <div className="pj-settings-group-cap">{g}</div>
                {items.map(s => (
                  <div
                    key={s.id}
                    className={`pj-settings-navitem ${section === s.id ? 'is-selected' : ''}`}
                    onClick={() => setSection(s.id)}
                  >{s.label}</div>
                ))}
              </div>
            ))}
          </aside>

          <main className="pj-settings-content">
            {section === 'general' && (
              <div>
                <p className="pj-hint">Benutzerdaten – werden in PDF-Kopf und -Fuß eingedruckt.</p>
                <FieldLabel>Name</FieldLabel>
                <TextField value={name} onChange={setName} />
                <FieldLabel>Firma</FieldLabel>
                <TextField value={firma} onChange={setFirma} />
              </div>
            )}
            {section === 'paths' && (
              <div>
                <p className="pj-hint">Verzeichnispfade können direkt eingegeben oder per Ordner-Browser ausgewählt werden.</p>
                {[
                  { k: 'quell',   l: 'Quellverzeichnis (MP3-Eingang)' },
                  { k: 'archiv',  l: 'Archivverzeichnis (verarbeitete MP3s)' },
                  { k: 'ausgabe', l: 'Ausgabeverzeichnis (JSON, PDF, HTML)' },
                ].map(({k, l}) => (
                  <React.Fragment key={k}>
                    <FieldLabel>{l}</FieldLabel>
                    <div className="pj-row-with-browse">
                      <TextField value={paths[k]} onChange={v => setPaths({...paths, [k]: v})} />
                      <button className="pj-action">Durchsuchen…</button>
                    </div>
                  </React.Fragment>
                ))}
              </div>
            )}
            {section !== 'general' && section !== 'paths' && (
              <div className="pj-prompt-editor">
                <p className="pj-hint">
                  {section === 'system'
                    ? 'Globale Rolle/Persönlichkeit für alle GPT-Aufrufe (system message).'
                    : `Prompt für die Sektion „${SECTIONS.find(s => s.id === section)?.label}". Platzhalter: {transcript}`}
                </p>
                <textarea className="pj-input pj-input-prompt" defaultValue={DEFAULT_PROMPT}></textarea>
              </div>
            )}
          </main>
        </div>

        <div className="pj-window-footer">
          <span className="pj-status-saved">{saved}</span>
          <div className="pj-window-footer-actions">
            <button className="pj-action">Standard wiederherstellen</button>
            <button className="pj-primary" onClick={() => {
              if (findings.f12) {
                onSaved && onSaved();
              } else {
                setSaved('Einstellungen gespeichert.');
                setTimeout(() => setSaved(''), 2000);
              }
            }}>
              Speichern
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

Object.assign(window, { SettingsDialog });
