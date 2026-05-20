/* DatePane.jsx — left column: red header + date list + sections checkboxes.
   v1.1.0 changes:
   · F03 — month group caps ("MÄRZ 2026"), count chip (·2) after each date.
   · F06 — "Im Eintrag anzeigen" label + standard hint + reset link.
*/

const SECTION_OPTS = [
  { key: 'showProse', label: 'Ausführlich' },
  { key: 'showLong', label: 'Zusammenfassung' },
  { key: 'showTasks', label: 'Aufgaben' },
  { key: 'showConv', label: 'Gesprächsnotiz' },
  { key: 'showEmail', label: 'E-Mail' },
  { key: 'showStunden', label: 'Stundenzettel' },
  { key: 'showAnalog', label: 'Analog' },
  { key: 'showTranscript', label: 'Transkript' },
];

const MONTHS_DE = [
  'Januar','Februar','März','April','Mai','Juni',
  'Juli','August','September','Oktober','November','Dezember',
];

const groupDatesByMonth = (dates) => {
  // dates are pre-sorted newest first by App
  const groups = [];
  let current = null;
  dates.forEach(d => {
    const [yyyy, mm] = d.key.split('-');
    const key = `${yyyy}-${mm}`;
    if (!current || current.key !== key) {
      current = { key, label: `${MONTHS_DE[parseInt(mm, 10) - 1]} ${yyyy}`, items: [] };
      groups.push(current);
    }
    current.items.push(d);
  });
  return groups;
};

const DatePane = ({
  dates, selectedDate, onSelectDate,
  sectionsState, onToggleSection, onResetSections,
  findings = {},
}) => {
  const groups = findings.f03 ? groupDatesByMonth(dates) : null;

  return (
    <div className="pj-pane pj-pane-date">
      <div className="pj-pane-header">Datum</div>
      <div className="pj-pane-scroll">
        {findings.f03 ? (
          groups.map(g => (
            <React.Fragment key={g.key}>
              <div className="pj-date-group-cap">{g.label}</div>
              {g.items.map(d => (
                <div
                  key={d.key}
                  className={`pj-date-item ${d.key === selectedDate ? 'is-selected' : ''} ${d.allDone ? 'is-all-done' : ''}`}
                  onClick={() => onSelectDate(d.key)}
                >
                  <span className="pj-date-label">{d.display.replace(/\.\d\d$/, '.')}</span>
                  {d.allDone
                    ? <span className="pj-date-count pj-date-done" title={`Alle ${d.total} Einträge erledigt`}>✓</span>
                    : <span className="pj-date-count" title={`${d.pending} von ${d.total} offen`}>·&nbsp;{d.pending}</span>}
                </div>
              ))}
            </React.Fragment>
          ))
        ) : (
          dates.map(d => (
            <div
              key={d.key}
              className={`pj-date-item ${d.key === selectedDate ? 'is-selected' : ''}`}
              onClick={() => onSelectDate(d.key)}
            >
              <span className="pj-date-label">{d.display}</span>
              {d.allDone
                ? <span className="pj-date-count pj-date-done">✓</span>
                : d.pending > 1 && <span className="pj-date-count">({d.pending})</span>}
            </div>
          ))
        )}
      </div>

      <div className="pj-pane-footer">
        {findings.f06 ? (
          <>
            <div className="pj-pane-cap">
              <span>Im Eintrag anzeigen</span>
              <span className="pj-pane-cap-hint">Standard: nach Typ</span>
            </div>
            {SECTION_OPTS.map(opt => (
              <label key={opt.key} className="pj-check">
                <input
                  type="checkbox"
                  checked={!!sectionsState[opt.key]}
                  onChange={() => onToggleSection(opt.key)}
                />
                {opt.label}
              </label>
            ))}
            <button className="pj-reset-link" onClick={onResetSections} type="button">
              ↻ Standard wiederherstellen
            </button>
          </>
        ) : (
          <>
            <div className="pj-pane-cap">Abschnitte</div>
            {SECTION_OPTS.map(opt => (
              <label key={opt.key} className="pj-check">
                <input
                  type="checkbox"
                  checked={!!sectionsState[opt.key]}
                  onChange={() => onToggleSection(opt.key)}
                />
                {opt.label}
              </label>
            ))}
          </>
        )}
      </div>
    </div>
  );
};

Object.assign(window, { DatePane });
