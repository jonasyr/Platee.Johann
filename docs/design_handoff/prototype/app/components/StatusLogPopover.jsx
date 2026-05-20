/* StatusLogPopover.jsx — floating 420px popover with running + completed items */

const StatusLogPopover = ({ items, onClose, onClear, onClearCompleted }) => (
  <>
    <div className="pj-popover-backdrop" onClick={onClose}></div>
    <div className="pj-popover" style={{ right: 80, top: 40 }}>
      <div className="pj-popover-head">
        <span className="pj-popover-title">Status-Log</span>
        <span className="pj-popover-actions">
          <button className="pj-mini" onClick={onClearCompleted}>Erledigte löschen</button>
          <button className="pj-mini" onClick={onClear}>Alle löschen</button>
        </span>
      </div>
      <div className="pj-popover-body">
        {items.length === 0 && <div className="pj-empty pj-empty-small">Noch keine Verarbeitungen.</div>}
        {items.map(it => (
          <div key={it.id} className="pj-log-row">
            <span className="pj-log-time">{it.time}</span>
            <span className="pj-log-msg">{it.message}</span>
            <span className="pj-log-right">
              {it.running
                ? <ProgressBar width={50} height={4} />
                : <span className={`pj-log-result ${it.error ? 'is-error' : 'is-ok'}`}>{it.result}</span>}
            </span>
          </div>
        ))}
      </div>
    </div>
  </>
);

Object.assign(window, { StatusLogPopover });
