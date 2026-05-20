/* NewEntryDialog.jsx — modal "Neuer Eintrag" */

const TYPE_OPTIONS = ['Aufgabe', 'Gesprächsnotiz', 'E-Mail', 'Projekt', 'Stundenzettel', 'Analog'];

const NewEntryDialog = ({ open, onClose, onCreate, findings = {} }) => {
  const [type, setType] = React.useState('Aufgabe');
  const [project, setProject] = React.useState('');
  const [title, setTitle] = React.useState('');
  const [content, setContent] = React.useState('');
  const [valid, setValid] = React.useState('');

  if (!open) return null;

  const handleSave = () => {
    if (!project.trim() || !title.trim()) {
      setValid('Projekt und Titel sind Pflichtfelder.');
      return;
    }
    onCreate({ type, project: project.trim(), title: title.trim(), content });
    setProject(''); setTitle(''); setContent(''); setValid('');
    onClose();
  };

  return (
    <div className="pj-modal-scrim" onClick={onClose}>
      <div className="pj-window pj-newentry" onClick={e => e.stopPropagation()}>
        <div className="pj-window-chrome">
          <img src="../assets/Johann_256.png" className="pj-window-chrome-icon" alt="" data-pjicon="" />
          <span className="pj-window-title">Neuer Eintrag</span>
          <div className="pj-window-chrome-controls">
            <button className="pj-tb-btn pj-tb-close" onClick={onClose} title="Schließen">✕</button>
          </div>
        </div>

        <div className="pj-newentry-body">
          <FieldLabel>Typ</FieldLabel>
          <Select value={type} options={TYPE_OPTIONS} onChange={setType} />

          <FieldLabel required>Projekt</FieldLabel>
          <TextField value={project} onChange={setProject} placeholder="z. B. Johann" />

          <FieldLabel required>Titel</FieldLabel>
          <TextField value={title} onChange={setTitle} placeholder="z. B. App anpassen" />

          <FieldLabel>Inhalt / Transkript</FieldLabel>
          <TextField multiline height={160} value={content} onChange={setContent}
            placeholder="Text eingeben — oder leer lassen, wenn nur ein Platzhalter angelegt werden soll." />

          {valid && <div className="pj-validation">{valid}</div>}
        </div>

        <div className="pj-window-footer">
          <span></span>
          <div className="pj-window-footer-actions">
            <button className="pj-action" onClick={onClose}>Abbrechen</button>
            <button className="pj-primary" onClick={handleSave}>Speichern</button>
          </div>
        </div>
      </div>
    </div>
  );
};

Object.assign(window, { NewEntryDialog });
