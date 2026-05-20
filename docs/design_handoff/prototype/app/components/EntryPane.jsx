/* EntryPane.jsx — center column: red date header + sort/filter rail + list + + Neues.
   v1.1.0 changes:
   · F05 — sort & filter on their own rows with CAPS rail labels; active
           sort button uses the brand-red affordance (not WPF blue), arrow
           merged into the active button.
   · F11 — list row carries "Typ · Dauer" instead of just type, killing the
           dupe with the detail-header type badge.
*/

const EntryPane = ({
  selectedDateDisplay, entries, selectedEntryId, onSelectEntry,
  sortBy, sortDir, onSortBy,
  pendingOnly, onTogglePending,
  onAddEntry,
  findings = {},
}) => {
  const arrow = sortDir === 'desc' ? '↓' : '↑';

  return (
    <div className="pj-pane pj-pane-entry">
      <div className="pj-pane-header">{selectedDateDisplay}</div>

      {findings.f05 ? (
        <div className="pj-pane-rail">
          <div className="pj-rail-row">
            <span className="pj-rail-cap">Sortieren</span>
            <button
              type="button"
              className={`pj-sort-btn ${sortBy === 'id' ? 'is-active' : ''}`}
              onClick={() => onSortBy('id')}
            >
              {sortBy === 'id' && <span className="pj-sort-arrow">{arrow}</span>}
              Nr
            </button>
            <button
              type="button"
              className={`pj-sort-btn ${sortBy === 'project' ? 'is-active' : ''}`}
              onClick={() => onSortBy('project')}
            >
              {sortBy === 'project' && <span className="pj-sort-arrow">{arrow}</span>}
              Projekt
            </button>
          </div>
          <div className="pj-rail-row">
            <span className="pj-rail-cap">Filter</span>
            <label className="pj-check pj-check-inline">
              <input type="checkbox" checked={pendingOnly} onChange={onTogglePending} />
              Nur unerledigte
            </label>
          </div>
        </div>
      ) : (
        <div className="pj-pane-rail">
          <div className="pj-rail-left">
            <span className="pj-rail-sort">↕</span>
            <button
              className={`pj-toggle ${sortBy === 'id' ? 'is-on' : ''}`}
              onClick={() => onSortBy('id')}
            >Nr {sortBy === 'id' && <span className="pj-toggle-arrow">↓</span>}</button>
            <button
              className={`pj-toggle ${sortBy === 'project' ? 'is-on' : ''}`}
              onClick={() => onSortBy('project')}
            >Projekt {sortBy === 'project' && <span className="pj-toggle-arrow">↓</span>}</button>
          </div>
          <label className="pj-check pj-check-inline">
            <input type="checkbox" checked={pendingOnly} onChange={onTogglePending} />
            Nur unerledigte
          </label>
        </div>
      )}

      <div className="pj-pane-scroll">
        {entries.length === 0
          ? <div className="pj-empty">Keine Einträge an diesem Tag.</div>
          : entries.map(e => (
            <div
              key={e.id}
              className={`pj-entry ${e.id === selectedEntryId ? 'is-selected' : ''}`}
              onClick={() => onSelectEntry(e.id)}
            >
              <div className="pj-entry-line">
                <span className="pj-entry-seq">{String(e.seq).padStart(3, '0')}</span>
                <span className="pj-entry-project">{e.project}</span>
                <span className="pj-entry-dash"> — </span>
                <span className="pj-entry-title">{e.title}</span>
                {e.done && <span className="pj-entry-done">✓</span>}
              </div>
              <div className="pj-entry-type">
                {findings.f11 ? `${e.type} · ${e.duration}` : e.type}
              </div>
            </div>
          ))}
      </div>

      <div className="pj-pane-footer pj-pane-footer-actions">
        <button className="pj-action" onClick={onAddEntry}>+ Neues Element</button>
      </div>
    </div>
  );
};

Object.assign(window, { EntryPane });
