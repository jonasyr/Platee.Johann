/* DetailPane.jsx — right column: entry detail w/ sections + action toolbar.
   v1.1.0 changes:
   · F01 — toolbar groups: Export-cluster (HTML/PDF/E-Mail/Kopieren) joined
           via shared borders, then a divider, then ↻ Neu generieren as a
           clearly distinct "write" action.
   · F02 — "Als erledigt markieren" lives in the detail header (right of
           title), not above the abstract; no more wrap-to-two-lines.
   · F04 — proper empty state (Johann mark + guidance) when no entry; the
           reprocess button accepts a `disabled` + `disabledReason` to demo
           the "no API key" tooltip.
   · F08 — handled in CSS (.f08-on adjusts section spacing & header style).
   · F10 — zoom buttons removed from this toolbar; they live in the
           StatusBar at the bottom of the app.
*/

const inline = (t) => {
  let parts = t.split(/(\*\*[^*]+\*\*|`[^`]+`)/g);
  return parts.map((p, i) => {
    if (p.startsWith('**')) return <strong key={i}>{p.slice(2, -2)}</strong>;
    if (p.startsWith('`'))  return <code key={i}>{p.slice(1, -1)}</code>;
    return <React.Fragment key={i}>{p}</React.Fragment>;
  });
};

const renderMd = (md) => {
  if (!md) return null;
  const lines = md.split('\n');
  const out = [];
  let ul = null, ol = null, table = null, quote = null;
  const flush = () => {
    if (ul) { out.push(<ul key={out.length} className="pj-md-ul">{ul}</ul>); ul = null; }
    if (ol) { out.push(<ol key={out.length} className="pj-md-ol">{ol}</ol>); ol = null; }
    if (table) {
      out.push(<table key={out.length} className="pj-md-table">{table}</table>);
      table = null;
    }
    if (quote) { out.push(<blockquote key={out.length} className="pj-md-quote">{quote}</blockquote>); quote = null; }
  };
  lines.forEach((line, i) => {
    if (line.trim() === '') { flush(); return; }
    if (line.trim().startsWith('|') && line.trim().endsWith('|')) {
      const cells = line.split('|').slice(1, -1).map(c => c.trim());
      if (cells.every(c => /^[-:]+$/.test(c))) return;
      if (!table) table = [<thead key="h"><tr>{cells.map((c, j) => <th key={j}>{inline(c)}</th>)}</tr></thead>, <tbody key="b"></tbody>];
      else {
        const body = table[1];
        const newBody = React.cloneElement(body, {}, [
          ...(body.props.children || []),
          <tr key={i}>{cells.map((c, j) => <td key={j}>{inline(c)}</td>)}</tr>,
        ]);
        table = [table[0], newBody];
      }
      return;
    }
    if (line.startsWith('> ')) {
      if (!quote) quote = [];
      quote.push(<p key={i}>{inline(line.slice(2))}</p>);
      return;
    } else if (quote) { flush(); }

    if (/^\s*- \[[ x]\]/i.test(line)) {
      const checked = /\[x\]/i.test(line);
      const text = line.replace(/^\s*- \[[ x]\]\s*/i, '');
      if (!ul) ul = [];
      ul.push(<li key={i} className="pj-md-task">
        <span className={`pj-md-checkbox ${checked ? 'is-checked' : ''}`}>{checked ? '✓' : ''}</span>
        <span className={checked ? 'pj-md-task-done' : ''}>{inline(text)}</span>
      </li>);
    } else if (line.startsWith('- ')) {
      if (!ul) ul = [];
      ul.push(<li key={i}>{inline(line.slice(2))}</li>);
    } else if (/^\d+\.\s/.test(line)) {
      if (!ol) ol = [];
      ol.push(<li key={i}>{inline(line.replace(/^\d+\.\s/, ''))}</li>);
    } else if (line.startsWith('**')) {
      flush();
      out.push(<p key={i} className="pj-md-strong">{inline(line)}</p>);
    } else {
      flush();
      out.push(<p key={i}>{inline(line)}</p>);
    }
  });
  flush();
  return out;
};

const SECTION_DEFS = [
  { state: 'showLong',       key: 'longSummary',      label: 'Zusammenfassung',          markdown: true },
  { state: 'showProse',      key: 'proseSummary',     label: 'Ausführliche Zusammenfassung' },
  { state: 'showTasks',      key: 'tasks',            label: 'Aufgaben',                 markdown: true },
  { state: 'showConv',       key: 'conversationNote', label: 'Gesprächsnotiz',           markdown: true },
  { state: 'showStunden',    key: 'stundenzettelText',label: 'Stundenzettel',            markdown: true },
  { state: 'showAnalog',     key: 'analogText',       label: 'Analog' },
  { state: 'showEmail',      key: 'emailText',        label: 'E-Mail' },
  { state: 'showTranscript', key: 'transcript',       label: 'Transkript',               quiet: true },
];

const DetailPane = ({
  entry, sectionsState, zoom,
  onZoomIn, onZoomOut, onToggleDone, onReprocess,
  findings = {},
  apiKeyMissing = false,
}) => {
  if (!entry) {
    if (findings.f04) {
      return (
        <div className="pj-pane pj-pane-detail">
          <div className="pj-empty-center">
            <img
              src="../assets/Johann_256.png"
              className="pj-empty-mark"
              alt=""
              style={{ width: 64, height: 64 }}
            />
            <div className="pj-empty-text">
              Wähle links ein Datum und einen Eintrag aus,
              um Abstract, Zusammenfassung und Aktionen zu sehen.
            </div>
          </div>
          <DetailToolbar
            findings={findings}
            apiKeyMissing={apiKeyMissing}
            disabled={true}
          />
        </div>
      );
    }
    return (
      <div className="pj-pane pj-pane-detail">
        <div className="pj-empty pj-empty-center">Eintrag auswählen</div>
        <DetailToolbar findings={findings} />
      </div>
    );
  }

  return (
    <div className="pj-pane pj-pane-detail">
      <div className="pj-detail-scroll">
        <div className="pj-detail-zoom" style={{ transform: `scale(${zoom})`, transformOrigin: 'top left', width: `${100/zoom}%` }}>

          {findings.f02 ? (
            <div className="pj-detail-head">
              <div className="pj-detail-head-meta">
                <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 2 }}>
                  <TypeBadge type={entry.type} />
                  <span className="pj-detail-project">{entry.project}</span>
                </div>
                <h1 className="pj-detail-title">{entry.title}</h1>
                <div className="pj-detail-meta">
                  {entry.date} · {entry.duration}
                </div>
              </div>
              <button
                className={`pj-done-header-btn ${entry.done ? 'is-done' : ''}`}
                onClick={onToggleDone}
                title={entry.done ? 'Erledigung rückgängig machen' : 'Diesen Eintrag als erledigt markieren'}
              >
                {entry.done
                  ? <><span className="check">✓</span> Erledigt — rückgängig</>
                  : <>✓ Als erledigt markieren</>}
              </button>
            </div>
          ) : (
            <>
              <div className="pj-detail-head">
                <TypeBadge type={entry.type} />
                <span className="pj-detail-project">{entry.project}</span>
              </div>
              <h1 className="pj-detail-title">{entry.title}</h1>
              <div className="pj-detail-meta">{entry.date} · {entry.duration}</div>
            </>
          )}

          <hr className="pj-divider" />

          {!findings.f02 && (
            <div className="pj-detail-done-row">
              <button className="pj-action pj-done-button" onClick={onToggleDone}>
                {entry.done ? '✓ Erledigt — rückgängig machen' : 'Als erledigt markieren'}
              </button>
            </div>
          )}

          {entry.abstract && (
            <section className="pj-section">
              <div className="pj-section-header">Abstract</div>
              <div className="pj-abstract">{entry.abstract}</div>
            </section>
          )}

          {SECTION_DEFS.map(def => {
            if (!sectionsState[def.state]) return null;
            const val = entry[def.key];
            return (
              <section className="pj-section" key={def.key}>
                <div className="pj-section-header">{def.label}</div>
                <div className={`pj-section-body ${def.quiet ? 'pj-section-quiet' : ''}`}>
                  {val
                    ? (def.markdown
                        ? renderMd(val)
                        : val.split('\n').map((line, i) => <p key={i}>{line}</p>))
                    : <p className="pj-section-empty">
                        — Dieser Abschnitt wurde für „{entry.type}"-Einträge nicht generiert.
                        Über <strong>↻ Neu generieren</strong> nachholen.
                      </p>}
                </div>
              </section>
            );
          })}
        </div>
      </div>
      <DetailToolbar
        zoom={zoom}
        onZoomIn={onZoomIn}
        onZoomOut={onZoomOut}
        onReprocess={onReprocess}
        findings={findings}
        apiKeyMissing={apiKeyMissing}
      />
    </div>
  );
};

const DetailToolbar = ({
  zoom = 1, onZoomIn, onZoomOut, onReprocess,
  findings = {}, apiKeyMissing = false, disabled = false,
}) => {
  const regenDisabled = disabled || (findings.f04 && apiKeyMissing);
  const regenTitle = regenDisabled
    ? (apiKeyMissing
        ? 'Kein API-Key in Einstellungen → KI hinterlegt.'
        : 'Eintrag auswählen, um Aktionen zu nutzen.')
    : 'Sektionen mit aktuellem Prompt neu erzeugen';

  return (
    <div className="pj-detail-toolbar">
      <div className="pj-detail-toolbar-left">
        <button className="pj-action pj-action-export" disabled={disabled}>HTML</button>
        <button className="pj-action pj-action-export" disabled={disabled}>PDF</button>
        <button className="pj-action pj-action-export" disabled={disabled}>E-Mail</button>
        <button className="pj-action pj-action-export" disabled={disabled}>Kopieren</button>
        {findings.f01 && <span className="pj-divider-vert"></span>}
        <button
          className="pj-action pj-action-regen"
          onClick={regenDisabled ? undefined : onReprocess}
          disabled={regenDisabled}
          title={regenTitle}
        >↻ Neu generieren</button>
      </div>
      {!findings.f10 && (
        <div className="pj-detail-toolbar-right">
          <button className="pj-zoom" onClick={onZoomOut} title="Verkleinern">−</button>
          <span className="pj-zoom-text">{Math.round(zoom * 100)}%</span>
          <button className="pj-zoom" onClick={onZoomIn} title="Vergrößern">+</button>
        </div>
      )}
    </div>
  );
};

Object.assign(window, { DetailPane });
