/* shared.jsx — tiny pieces reused across panes */
/* exposes: TypeBadge, IconBtn, ProgressBar, Pill, FieldLabel */

const TypeBadge = ({ type }) => (
  <span className="pj-typebadge">{type}</span>
);

const Pill = ({ tone = 'gray', children }) => (
  <span className={`pj-pill pj-pill-${tone}`}>{children}</span>
);

const IconBtn = ({ children, onClick, tooltip, active, ariaLabel }) => (
  <button
    className={`pj-iconbtn ${active ? 'is-active' : ''}`}
    onClick={onClick}
    title={tooltip}
    aria-label={ariaLabel || tooltip}
  >{children}</button>
);

const ProgressBar = ({ width = 80, height = 8 }) => (
  <div className="pj-progress" style={{ width, height }}>
    <div className="pj-progress-bar"></div>
  </div>
);

const FieldLabel = ({ children, required }) => (
  <div className="pj-field-label">
    {children}{required && ' *'}
  </div>
);

const TextField = ({ value, onChange, placeholder, multiline, height }) => (
  multiline
    ? <textarea
        className="pj-input"
        value={value}
        onChange={e => onChange?.(e.target.value)}
        placeholder={placeholder}
        style={{ height }}
      />
    : <input
        className="pj-input"
        value={value}
        onChange={e => onChange?.(e.target.value)}
        placeholder={placeholder}
      />
);

/* Anchored, fully-styled dropdown — replaces native <select> which sometimes
   pops at unpredictable positions / sizes inside iframes & modals. */
const Select = ({ value, options, onChange }) => {
  const [open, setOpen] = React.useState(false);
  const wrapRef = React.useRef(null);

  React.useEffect(() => {
    if (!open) return;
    const close = (e) => { if (!wrapRef.current?.contains(e.target)) setOpen(false); };
    document.addEventListener('mousedown', close);
    return () => document.removeEventListener('mousedown', close);
  }, [open]);

  return (
    <div className="pj-select" ref={wrapRef}>
      <button
        type="button"
        className="pj-select-trigger"
        onClick={() => setOpen(o => !o)}
      >
        <span>{value}</span>
        <span className="pj-select-caret">▾</span>
      </button>
      {open && (
        <div className="pj-select-menu">
          {options.map(opt => (
            <div
              key={opt}
              className={`pj-select-option ${opt === value ? 'is-selected' : ''}`}
              onClick={() => { onChange(opt); setOpen(false); }}
            >{opt}</div>
          ))}
        </div>
      )}
    </div>
  );
};

Object.assign(window, { TypeBadge, Pill, IconBtn, ProgressBar, FieldLabel, TextField, Select });
