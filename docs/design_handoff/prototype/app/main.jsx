/* main.jsx — top-level App for the Platé.Johann desktop kit (v1.1.0).
   Implements every audit finding behind individual `f01..f12` tweaks.
   Defaults to all-on; flip any toggle off in the Tweaks panel to compare
   the v1.0.7 behaviour for that single change.
*/

const { useState, useMemo, useEffect, useCallback, useRef } = React;

// ── Defaults persisted by the host. ALL findings ship enabled. ───────────
const TWEAK_DEFAULTS = /*EDITMODE-BEGIN*/{
  "f01": true,
  "f02": true,
  "f03": true,
  "f04": true,
  "f05": true,
  "f06": true,
  "f07": true,
  "f08": true,
  "f09": true,
  "f10": true,
  "f11": true,
  "f12": true,
  "demoApiMissing": false,
  "demoPathMissing": false,
  "demoEmptySelection": false
}/*EDITMODE-END*/;

const DEFAULT_SECTIONS = {
  showLong: true, showProse: true, showTasks: false, showConv: false,
  showEmail: false, showStunden: false, showAnalog: false, showTranscript: false,
};

const sectionsForType = (type) => {
  const base = { ...DEFAULT_SECTIONS };
  if (type === 'Aufgabe')         base.showTasks = true;
  if (type === 'Gesprächsnotiz')  base.showConv = true;
  if (type === 'E-Mail')          base.showEmail = true;
  if (type === 'Stundenzettel')   base.showStunden = true;
  if (type === 'Analog')          base.showAnalog = true;
  return base;
};

// ── Status bar (F10) ──────────────────────────────────────────────────────
const StatusBar = ({ zoom, onZoomIn, onZoomOut, statusLeft, ausgabe = 'Dokumente\\Johann\\output', whisper = 'Whisper 1.2.3', auditNote }) => (
  <div className="pj-statusbar" role="status" aria-label="Statusleiste">
    <div className="sb-left">
      <span className="sb-item">{statusLeft || 'Bereit'}</span>
      <span className="sb-dot">·</span>
      <span className="sb-item" title="Ausgabe-Verzeichnis">Ausgabe in <code>{ausgabe}</code></span>
      <span className="sb-dot">·</span>
      <span className="sb-item">{whisper}</span>
      {auditNote && (
        <>
          <span className="sb-dot">·</span>
          <span className="sb-item sb-audit" title="UX-Audit-Status dieses Prototyps">
            <span className="sb-audit-dot"></span>
            {auditNote}
          </span>
        </>
      )}
    </div>
    <div className="sb-right">
      <button className="sb-zoom-btn" onClick={onZoomOut} title="Verkleinern" aria-label="Verkleinern">−</button>
      <span className="sb-zoom-val">{Math.round(zoom * 100)}%</span>
      <button className="sb-zoom-btn" onClick={onZoomIn} title="Vergrößern" aria-label="Vergrößern">+</button>
    </div>
  </div>
);

// ── Toast tray (F12) ──────────────────────────────────────────────────────
const TOAST_ICONS = { ok: '✓', warn: '⚠', error: '!' };

const ToastTray = ({ toasts, onDismiss, onLogClick }) => (
  <div className="pj-toast-tray" aria-live="polite">
    {toasts.map(t => (
      <div key={t.id} className={`pj-toast toast-${t.tone}`}>
        <span className="ico" aria-hidden="true">{TOAST_ICONS[t.tone]}</span>
        <div className="body">
          <div className="ttl">{t.title}</div>
          {t.message && <div className="msg">{t.message}</div>}
          {t.tone === 'error' && (
            <button className="toast-link" onClick={onLogClick} type="button">Details im Status-Log</button>
          )}
        </div>
        <button
          className="toast-close"
          onClick={() => onDismiss(t.id)}
          title="Schließen"
          aria-label="Schließen"
        >×</button>
      </div>
    ))}
  </div>
);

// ── Tweaks panel content ──────────────────────────────────────────────────
const FINDINGS = [
  { id: 'f01', label: '01 · Footer-Cluster',         prio: 'Hoch' },
  { id: 'f02', label: '02 · „Erledigt" im Header',   prio: 'Hoch' },
  { id: 'f03', label: '03 · Datum mit Monatskopf',   prio: 'Hoch' },
  { id: 'f04', label: '04 · Leerzustand & Tooltips', prio: 'Hoch' },
  { id: 'f05', label: '05 · Sort-Header',            prio: 'Mittel' },
  { id: 'f06', label: '06 · Abschnitte umlabeln',    prio: 'Mittel' },
  { id: 'f07', label: '07 · Einstellungen sauber',   prio: 'Mittel' },
  { id: 'f08', label: '08 · Sektions-Rhythmus',      prio: 'Mittel' },
  { id: 'f09', label: '09 · Tooltips & Bell-Dot',    prio: 'Mittel' },
  { id: 'f10', label: '10 · Zoom in Statuszeile',    prio: 'Niedrig' },
  { id: 'f11', label: '11 · Typ + Dauer in Liste',   prio: 'Niedrig' },
  { id: 'f12', label: '12 · Toast-Varianten',        prio: 'Niedrig' },
];

const AuditTweaks = ({ t, setT, pushToast }) => {
  const allOn  = FINDINGS.every(f => t[f.id]);
  const allOff = FINDINGS.every(f => !t[f.id]);

  const setAll = (v) => {
    const patch = {};
    FINDINGS.forEach(f => { patch[f.id] = v; });
    setT(patch);
  };

  return (
    <TweaksPanel title="UX-Audit · Tweaks">
      <TweakSection label="Schnellumschalter" />
      <TweakRadio
        label="Alle Befunde"
        value={allOn ? 'Nachher' : allOff ? 'Vorher' : 'Mix'}
        options={['Vorher', 'Mix', 'Nachher']}
        onChange={v => {
          if (v === 'Vorher')  setAll(false);
          if (v === 'Nachher') setAll(true);
          // 'Mix' is informational — leave individual state untouched
        }}
      />

      <TweakSection label="Hoch" />
      {FINDINGS.filter(f => f.prio === 'Hoch').map(f => (
        <TweakToggle key={f.id} label={f.label} value={!!t[f.id]} onChange={v => setT(f.id, v)} />
      ))}

      <TweakSection label="Mittel" />
      {FINDINGS.filter(f => f.prio === 'Mittel').map(f => (
        <TweakToggle key={f.id} label={f.label} value={!!t[f.id]} onChange={v => setT(f.id, v)} />
      ))}

      <TweakSection label="Niedrig" />
      {FINDINGS.filter(f => f.prio === 'Niedrig').map(f => (
        <TweakToggle key={f.id} label={f.label} value={!!t[f.id]} onChange={v => setT(f.id, v)} />
      ))}

      <TweakSection label="Demo-Zustände" />
      <TweakToggle
        label="API-Key fehlt"
        value={!!t.demoApiMissing}
        onChange={v => setT('demoApiMissing', v)}
      />
      <TweakToggle
        label="Eingangs-Ordner fehlt"
        value={!!t.demoPathMissing}
        onChange={v => setT('demoPathMissing', v)}
      />
      <TweakToggle
        label="Kein Eintrag ausgewählt"
        value={!!t.demoEmptySelection}
        onChange={v => setT('demoEmptySelection', v)}
      />

      <TweakSection label="Toast-Test (F12)" />
      <TweakButton label="OK-Toast"    onClick={() => pushToast({ tone: 'ok',    title: 'Eintrag gespeichert.',          message: '003 · Albrecht — Rückfrage zum Wartungsvertrag' })} />
      <TweakButton label="Warn-Toast"  onClick={() => pushToast({ tone: 'warn',  title: 'KI-Schritt übersprungen.',     message: 'Kein API-Key — Transkript wurde gesichert, aber nicht zusammengefasst.' })} secondary />
      <TweakButton label="Fehler-Toast"onClick={() => pushToast({ tone: 'error', title: 'Whisper-Aufruf fehlgeschlagen.', message: 'Verbindung verloren. Datei verbleibt im Eingangs-Ordner.' })} secondary />
    </TweaksPanel>
  );
};

// ── App ───────────────────────────────────────────────────────────────────
function App() {
  const [t, setT] = useTweaks(TWEAK_DEFAULTS);

  const [entries, setEntries] = useState(window.PJ_DATA.entries);

  const dates = useMemo(() => {
    const stats = new Map();
    entries.forEach(e => {
      const s = stats.get(e.dateKey) || { total: 0, pending: 0 };
      s.total += 1;
      if (!e.done) s.pending += 1;
      stats.set(e.dateKey, s);
    });
    return [...stats.entries()]
      .sort(([a], [b]) => (a < b ? 1 : -1))
      .map(([key, s]) => {
        const [y, m, d] = key.split('-');
        return {
          key,
          display: `${d}.${m}.${y.slice(2)}`,
          total: s.total,
          pending: s.pending,
          count: s.total,
          allDone: s.pending === 0,
        };
      });
  }, [entries]);

  const [selectedDate, setSelectedDate]       = useState(dates[0]?.key);
  const [selectedEntryId, setSelectedEntryId] = useState(entries[0]?.id);
  const [sortBy, setSortBy]                   = useState('id');
  const [sortDir, setSortDir]                 = useState('asc');
  const [pendingOnly, setPendingOnly]         = useState(false);
  const [sectionsState, setSectionsState]     = useState(sectionsForType(entries[0]?.type));
  const [zoom, setZoom]                       = useState(1);
  const [logOpen, setLogOpen]                 = useState(false);
  const [settingsOpen, setSettingsOpen]       = useState(false);
  const [newOpen, setNewOpen]                 = useState(false);

  const [log, setLog] = useState([
    { id: 1, time: '14:01', message: '002 · Iris — Gesprächsnotiz erstellt', running: false, result: 'Fertig (3.2 s)' },
    { id: 2, time: '13:58', message: '003 · Müller — E-Mail kopiert',         running: false, result: 'Fertig' },
  ]);
  const [isProcessing, setIsProcessing] = useState(false);
  const [statusText, setStatusText]     = useState('');

  // Toast queue (F12)
  const [toasts, setToasts] = useState([]);
  const toastTimers = useRef(new Map());

  const dismissToast = useCallback((id) => {
    setToasts(arr => arr.filter(x => x.id !== id));
    const h = toastTimers.current.get(id);
    if (h) { clearTimeout(h); toastTimers.current.delete(id); }
  }, []);

  const pushToast = useCallback((toast) => {
    if (!t.f12) return; // before-state: no toast system
    const id = Date.now() + Math.random();
    setToasts(arr => [...arr, { id, tone: 'ok', ...toast }]);
    const h = setTimeout(() => {
      setToasts(arr => arr.filter(x => x.id !== id));
      toastTimers.current.delete(id);
    }, 5200);
    toastTimers.current.set(id, h);
  }, [t.f12]);

  const selectedDateDisplay = useMemo(() => {
    const d = dates.find(x => x.key === selectedDate);
    if (!d) return '';
    const [y, m, day] = selectedDate.split('-');
    return `${day}.${m}.${y}`;
  }, [selectedDate, dates]);

  const visibleEntries = useMemo(() => {
    let list = entries.filter(e => e.dateKey === selectedDate);
    if (pendingOnly) list = list.filter(e => !e.done);
    const factor = sortDir === 'desc' ? -1 : 1;
    list.sort((a, b) => factor * (sortBy === 'project'
      ? a.project.localeCompare(b.project)
      : a.seq - b.seq));
    return list;
  }, [entries, selectedDate, sortBy, sortDir, pendingOnly]);

  const selectedEntry = (!t.demoEmptySelection && entries.find(e => e.id === selectedEntryId))
    || (!t.demoEmptySelection && visibleEntries[0])
    || null;

  useEffect(() => {
    if (selectedEntry) setSectionsState(sectionsForType(selectedEntry.type));
  }, [selectedEntry?.id]);

  const handleSelectDate = (key) => {
    setSelectedDate(key);
    const first = entries.find(e => e.dateKey === key);
    if (first) setSelectedEntryId(first.id);
  };

  const handleSortBy = (key) => {
    if (key === sortBy) setSortDir(d => d === 'asc' ? 'desc' : 'asc');
    else { setSortBy(key); setSortDir('asc'); }
  };

  const handleResetSections = () => {
    if (selectedEntry) setSectionsState(sectionsForType(selectedEntry.type));
    pushToast({ tone: 'ok', title: 'Abschnitts-Auswahl zurückgesetzt.' });
  };

  const handleReprocess = () => {
    if (t.demoApiMissing) {
      pushToast({
        tone: 'warn',
        title: 'KI-Aufruf nicht möglich.',
        message: 'Kein API-Key in Einstellungen → KI hinterlegt.',
      });
      return;
    }
    setIsProcessing(true);
    setStatusText('GPT-Abschnitte neu generieren …');
    const id = Date.now();
    const time = new Date().toTimeString().slice(0, 5);
    setLog(l => [{ id, time, message: `${String(selectedEntry.seq).padStart(3, '0')} · ${selectedEntry.project} — neu verarbeitet`, running: true, result: '' }, ...l]);
    setTimeout(() => {
      setLog(l => l.map(x => x.id === id ? { ...x, running: false, result: 'Fertig (2.1 s)' } : x));
      setIsProcessing(false);
      setStatusText('');
      pushToast({ tone: 'ok', title: 'Sektionen neu erzeugt.', message: `${String(selectedEntry.seq).padStart(3, '0')} · ${selectedEntry.project} — ${selectedEntry.title}` });
    }, 2200);
  };

  const handleToggleDone = () => {
    setEntries(es => es.map(e => e.id === selectedEntry.id ? { ...e, done: !e.done } : e));
    pushToast({
      tone: 'ok',
      title: selectedEntry.done ? 'Erledigung rückgängig gemacht.' : 'Eintrag als erledigt markiert.',
      message: `${String(selectedEntry.seq).padStart(3, '0')} · ${selectedEntry.project} — ${selectedEntry.title}`,
    });
  };

  const handleCreate = ({ type, project, title, content }) => {
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    const dateKey = `${yyyy}-${mm}-${dd}`;
    const date = `${dd}.${mm}.${yyyy}`;
    const seq = (entries.filter(e => e.dateKey === dateKey).length || 0) + 1;
    const id = 'n' + Date.now();
    const entry = {
      id, dateKey, date, seq, type, project, title,
      duration: '–', done: false,
      transcript: content,
      abstract: content.slice(0, 80) + (content.length > 80 ? '…' : ''),
      proseSummary: content,
    };
    setEntries(es => [entry, ...es]);
    setSelectedDate(dateKey);
    setSelectedEntryId(id);

    setIsProcessing(true);
    setStatusText(`Verarbeite ${type.toLowerCase()} „${title}" …`);
    const logId = Date.now() + 1;
    const time = new Date().toTimeString().slice(0, 5);
    setLog(l => [{ id: logId, time, message: `${String(seq).padStart(3, '0')} · ${project} — wird verarbeitet`, running: true, result: '' }, ...l]);
    setTimeout(() => {
      setLog(l => l.map(x => x.id === logId ? { ...x, running: false, result: 'Fertig (1.8 s)' } : x));
      setIsProcessing(false);
      setStatusText('');
      pushToast({ tone: 'ok', title: 'Eintrag gespeichert.', message: `${String(seq).padStart(3, '0')} · ${project} — ${title}` });
    }, 1800);
  };

  // ── Compose root class list ─────────────────────────────────────────────
  const rootClass = useMemo(() => {
    const cls = ['pj-app'];
    FINDINGS.forEach(f => { if (t[f.id]) cls.push(`${f.id}-on`); });
    return cls.join(' ');
  }, [t]);

  const findingsObj = useMemo(() => {
    const o = {};
    FINDINGS.forEach(f => { o[f.id] = !!t[f.id]; });
    return o;
  }, [t]);

  const bellHasRunning = log.some(x => x.running);

  // ── Render ──────────────────────────────────────────────────────────────
  return (
    <div className={rootClass}>
      <div className="pj-titlebar">
        <img src="../assets/Johann_256.png" className="pj-titlebar-icon" alt="" />
        <span className="pj-titlebar-text">Platé.Johann v1.1.0</span>
        <div className="pj-titlebar-controls">
          <button className="pj-tb-btn" title="Minimieren">−</button>
          <button className="pj-tb-btn" title="Maximieren">□</button>
          <button className="pj-tb-btn pj-tb-close" title="Schließen">✕</button>
        </div>
      </div>

      <TopBar
        isProcessing={isProcessing}
        statusText={statusText}
        onOpenLog={() => setLogOpen(o => !o)}
        onOpenSettings={() => setSettingsOpen(true)}
        logOpen={logOpen}
        findings={findingsObj}
        bellHasRunning={bellHasRunning}
      />

      {t.f04 && t.demoPathMissing && (
        <div className="pj-banner banner-error">
          <span className="ico" aria-hidden="true">⚠</span>
          <span>
            Eingangs-Ordner nicht gefunden:&nbsp;
            <code style={{ background: 'transparent', padding: 0, fontFamily: 'var(--pj-font-mono)' }}>Dokumente\Johann\Eingang</code>
          </span>
          <a onClick={() => setSettingsOpen(true)}>Pfad ändern</a>
        </div>
      )}
      {t.f04 && t.demoApiMissing && (
        <div className="pj-banner">
          <span className="ico" aria-hidden="true">ℹ</span>
          <span>Kein OpenAI-Key gesetzt — KI-Aktionen sind ausgegraut.</span>
          <a onClick={() => setSettingsOpen(true)}>Key hinterlegen</a>
        </div>
      )}

      <div className="pj-three-col">
        <DatePane
          dates={pendingOnly ? dates.filter(d => d.pending > 0) : dates}
          selectedDate={selectedDate}
          onSelectDate={handleSelectDate}
          sectionsState={sectionsState}
          onToggleSection={k => setSectionsState(s => ({ ...s, [k]: !s[k] }))}
          onResetSections={handleResetSections}
          findings={findingsObj}
        />
        <div className="pj-splitter"></div>
        <EntryPane
          selectedDateDisplay={selectedDateDisplay}
          entries={visibleEntries}
          selectedEntryId={selectedEntry?.id}
          onSelectEntry={setSelectedEntryId}
          sortBy={sortBy}
          sortDir={sortDir}
          onSortBy={handleSortBy}
          pendingOnly={pendingOnly}
          onTogglePending={() => setPendingOnly(p => !p)}
          onAddEntry={() => setNewOpen(true)}
          findings={findingsObj}
        />
        <div className="pj-splitter"></div>
        <DetailPane
          entry={selectedEntry}
          sectionsState={sectionsState}
          zoom={zoom}
          onZoomIn={() => setZoom(z => Math.min(1.6, +(z + 0.1).toFixed(2)))}
          onZoomOut={() => setZoom(z => Math.max(0.6, +(z - 0.1).toFixed(2)))}
          onToggleDone={handleToggleDone}
          onReprocess={handleReprocess}
          findings={findingsObj}
          apiKeyMissing={t.demoApiMissing}
        />
      </div>

      {t.f10 && (
        <StatusBar
          zoom={zoom}
          onZoomIn={() => setZoom(z => Math.min(1.6, +(z + 0.1).toFixed(2)))}
          onZoomOut={() => setZoom(z => Math.max(0.6, +(z - 0.1).toFixed(2)))}
          statusLeft={isProcessing ? statusText : (t.demoPathMissing ? 'Eingangs-Ordner offline' : 'Bereit')}
          auditNote={`UX-Audit · ${Object.keys(findingsObj).filter(k => findingsObj[k]).length}/12 Befunde aktiv`}
        />
      )}

      {logOpen && (
        <StatusLogPopover
          items={log}
          onClose={() => setLogOpen(false)}
          onClear={() => setLog([])}
          onClearCompleted={() => setLog(l => l.filter(x => x.running))}
        />
      )}

      <SettingsDialog
        open={settingsOpen}
        onClose={() => setSettingsOpen(false)}
        findings={findingsObj}
        onSaved={() => pushToast({ tone: 'ok', title: 'Einstellungen gespeichert.' })}
      />
      <NewEntryDialog
        open={newOpen}
        onClose={() => setNewOpen(false)}
        onCreate={handleCreate}
        findings={findingsObj}
      />

      <ToastTray
        toasts={toasts}
        onDismiss={dismissToast}
        onLogClick={() => { setLogOpen(true); }}
      />

      <AuditTweaks t={t} setT={setT} pushToast={pushToast} />
    </div>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<App />);
