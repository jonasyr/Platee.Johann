/* TopBar.jsx — 36px chrome strip with status + bell + settings + help.
   v1.1.0 changes:
   · F09 — bell carries a red dot when log has running items;
           help/bell have explicit titles + aria-labels.
*/

const TopBar = ({
  isProcessing, statusText,
  errorMessage,
  onOpenLog, onOpenSettings,
  logOpen,
  findings = {},
  bellHasRunning = false,
}) => (
  <div className="pj-topbar">
    <div className="pj-topbar-error">
      {errorMessage}
    </div>
    <div className="pj-topbar-right">
      {isProcessing && (
        <>
          <span className="pj-topbar-status">{statusText}</span>
          <ProgressBar />
        </>
      )}
      <IconBtn
        onClick={onOpenLog}
        tooltip={findings.f09 ? 'Status-Log öffnen (F2)' : 'Status-Log öffnen'}
        ariaLabel="Status-Log"
        active={logOpen}
      >
        🔔
        {findings.f09 && bellHasRunning && <span className="pj-bell-dot" aria-hidden="true"></span>}
      </IconBtn>
      <button
        className="pj-ghost"
        onClick={onOpenSettings}
        title={findings.f09 ? 'Einstellungen öffnen (Strg+,)' : 'Einstellungen öffnen'}
        aria-label="Einstellungen öffnen"
      >
        <span className="pj-ghost-icon">⚙</span> Einstellungen
      </button>
      <button
        className="pj-ghost pj-ghost-help"
        title={findings.f09 ? 'Handbuch öffnen (F1)' : 'Hilfe öffnen'}
        aria-label="Handbuch öffnen"
      >?</button>
    </div>
  </div>
);

Object.assign(window, { TopBar });
