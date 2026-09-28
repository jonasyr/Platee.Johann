# Erfundene Diktate fuer die Kosten-Kalibrierung (#71 Stufe 2).
# Keine echten Inhalte, keine echten Personen oder Kunden.
# Sprechrate aus dem echten Archiv abgeleitet: ~128 Transkript-Token je Minute.

DICTATIONS = [
    # --- sehr kurz: Zuruf zwischen Tuer und Angel (~20-40 s) ---
    ("Kurznotiz Rueckruf",
     "Kurz zur Erinnerung: Frau Hartmann von der Stadtverwaltung hat angerufen, "
     "sie braucht die Bestaetigung fuer den Termin am Donnerstag. Bitte bis morgen "
     "Mittag zurueckrufen, Durchwahl steht im Verteiler."),

    ("Materialbestellung",
     "Wir brauchen fuer die Baustelle Nordring noch zwanzig Meter Kabelkanal "
     "in vierzig mal sechzig und zwei Rollen Installationsdraht. Bitte heute noch "
     "bestellen, damit es Freitag da ist."),

    ("Zeiterfassung kurz",
     "Zeiterfassung fuer heute: acht Uhr dreissig bis zwoelf Uhr Baustelle Nordring, "
     "Montage Unterverteilung. Danach dreizehn bis sechzehn Uhr Buero, Angebotsbearbeitung "
     "Projekt Lueftung Bauteil C."),

    # --- kurz: einfache Gespraechsnotiz (~1 min) ---
    ("Telefonat Angebot",
     "Notiz zum Telefonat mit Herrn Vogel von der Firma Brenner heute kurz nach neun. "
     "Er hat nachgefragt, wann das Angebot fuer die Sanierung im Erdgeschoss kommt. "
     "Ich habe gesagt, dass wir bis Ende der Woche liefern koennen, weil noch zwei "
     "Preise vom Lieferanten fehlen. Er war einverstanden, moechte aber vorab schon "
     "die grobe Groessenordnung wissen. Ich habe ihm gesagt, wir liegen vermutlich "
     "zwischen achtzehn und zweiundzwanzigtausend Euro netto, ohne Malerarbeiten. "
     "Das muss ich aber noch mit Kollegin Neele gegenrechnen."),

    ("Baustellenbegehung kurz",
     "Begehung Baustelle Nordring, heute Vormittag. Der Estrich im ersten Obergeschoss "
     "ist fertig und trocken, wir koennen ab Montag mit der Verlegung anfangen. "
     "Im Treppenhaus fehlt noch das Gelaender, das haengt am Schlosser. "
     "Ausserdem ist mir aufgefallen, dass die Bauheizung nachts durchlaeuft, "
     "das sollten wir abstellen, sonst wird die Abrechnung unangenehm. "
     "Bitte den Bauleiter darauf ansprechen."),

    ("Aufgaben Tagesabschluss",
     "Zusammenfassung fuer heute Abend. Erledigt: Angebot Brenner vorbereitet, "
     "Aufmass Nordring erster Stock genommen, Bestellung Kabelkanal raus. "
     "Offen geblieben: Rueckruf Frau Hartmann, die Pruefprotokolle vom letzten "
     "Monat sind noch nicht abgelegt, und der Termin mit dem Statiker fuer "
     "Bauteil C muss noch gemacht werden. Das nehme ich morgen frueh mit."),

    # --- mittel: richtige Besprechung (~2 min) ---
    ("Besprechung Lueftungsanlage",
     "Gespraechsnotiz zur Besprechung mit der Firma Bergmann heute Vormittag, "
     "zehn bis elf Uhr, bei uns im Haus. Anwesend waren Herr Bergmann, seine "
     "technische Leiterin und ich. Thema war die Erneuerung der Lueftungsanlage "
     "im Bauteil C. Der Kunde moechte die Arbeiten in den Betriebsferien im August "
     "durchfuehren lassen, weil dann die Produktion stillsteht und wir freien Zugang "
     "haben. Das sind drei Wochen, das sollte reichen, ist aber knapp, wenn die "
     "Geraete nicht rechtzeitig geliefert werden. Ich habe zugesagt, bis Ende naechster "
     "Woche ein grobes Angebot zu schicken, mit zwei Varianten: einmal nur der Austausch "
     "der vorhandenen Geraete, einmal zusaetzlich mit Waermerueckgewinnung. "
     "Herr Bergmann hat angemerkt, dass das Budget bei etwa achtzigtausend Euro liegt, "
     "das sollten wir im Blick behalten. Die zweite Variante wird da vermutlich "
     "drueberliegen, das muss ich sauber darstellen. Ausserdem braucht er vorab eine "
     "Aufstellung, welche Bereiche waehrend der Arbeiten nicht zugaenglich sind, "
     "weil er das mit dem Betriebsrat abstimmen muss. Die Elektrikerarbeiten muessen "
     "wir mit Kollegin Neele abstimmen, sie kennt die Verteilung im Bauteil C am besten. "
     "Naechster Termin ist der zwoelfte Maerz um vierzehn Uhr bei uns im Haus."),

    ("Schadensaufnahme",
     "Aufnahme Wasserschaden Objekt Lindenstrasse, Aufgang zwei. "
     "Der Schaden ist in der Wohnung im zweiten Obergeschoss aufgetreten, "
     "offenbar ist die Anschlussleitung der Spuelmaschine ueber Nacht undicht geworden. "
     "Betroffen sind die Kueche komplett, der Flur teilweise und die darunterliegende "
     "Wohnung im ersten Obergeschoss an der Decke. Der Estrich ist durchfeuchtet, "
     "die Messung hat im Kuechenbereich Werte deutlich ueber dem Grenzwert ergeben. "
     "Wir brauchen also eine Trocknung, ich schaetze zwei bis drei Wochen. "
     "Die Mieterin ist verstaendlicherweise aufgebracht, weil sie die Kueche nicht "
     "nutzen kann. Ich habe ihr zugesagt, dass wir uns morgen wegen einer Zwischenloesung "
     "melden. Die Hausverwaltung ist informiert, die Versicherung noch nicht, "
     "das muss ich morgen frueh machen. Fotos sind gemacht und liegen im Ordner. "
     "Wichtig: die Trocknungsgeraete muessen ueber den Zwischenzaehler laufen, "
     "sonst gibt es bei der Stromabrechnung wieder Streit wie beim letzten Mal."),

    ("Projektstatus",
     "Statusdiktat Projekt Nordring, Stand heute. Der Rohbau ist abgeschlossen und "
     "abgenommen, kleinere Restpunkte sind im Protokoll festgehalten. Die Gebaeudetechnik "
     "liegt etwa eine Woche hinter dem Plan, hauptsaechlich weil die Lueftungskanaele "
     "spaeter geliefert wurden als zugesagt. Das koennen wir aufholen, wenn die "
     "Elektromontage parallel laeuft, das muss ich mit dem Bauleiter klaeren. "
     "Die Kosten liegen aktuell etwa drei Prozent ueber dem Ansatz, im Wesentlichen "
     "wegen der Mehrmengen beim Estrich. Das ist vertretbar, sollte aber im naechsten "
     "Jour fixe angesprochen werden, damit es niemanden ueberrascht. "
     "Kritisch ist der Termin fuer die Inbetriebnahme im Juni, weil daran die "
     "Abnahme durch den Sachverstaendigen haengt und der lange Vorlaufzeiten hat. "
     "Ich wuerde vorschlagen, den Termin jetzt schon anzufragen, auch auf die Gefahr hin, "
     "dass wir ihn nochmal verschieben muessen."),

    # --- laenger: ausfuehrliche Notiz (~3-4 min) ---
    ("Jour fixe ausfuehrlich",
     "Gespraechsnotiz Jour fixe Projekt Nordring, Donnerstag vierzehn Uhr, "
     "Baubuero vor Ort. Teilnehmer waren der Bauherrenvertreter, der Architekt, "
     "der Bauleiter und ich fuer die technische Gebaeudeausruestung. "
     "Erster Punkt war der Terminplan. Der Bauleiter hat dargestellt, dass wir bei "
     "der Gebaeudetechnik etwa eine Woche im Verzug sind. Ursache ist die verspaetete "
     "Lieferung der Lueftungskanaele, die urspruenglich fuer Mitte des Monats zugesagt "
     "war und jetzt erst kommende Woche kommt. Der Architekt hat gefragt, ob das den "
     "Innenausbau beeinflusst. Aus meiner Sicht nicht, solange wir die Abhangdecken "
     "erst danach schliessen, aber das muss sauber koordiniert werden, sonst muessen "
     "wir zweimal aufmachen. Zweiter Punkt waren die Mehrkosten. Wir liegen aktuell "
     "etwa drei Prozent ueber dem Ansatz, hauptsaechlich Mehrmengen beim Estrich und "
     "eine Nachtragsposition fuer die zusaetzlichen Brandschotts, die bei der Planung "
     "nicht beruecksichtigt waren. Der Bauherrenvertreter hat darum gebeten, das "
     "schriftlich aufzubereiten, mit Gegenueberstellung Ansatz und aktueller Prognose. "
     "Das uebernehme ich bis naechsten Mittwoch. Dritter Punkt war die Inbetriebnahme. "
     "Der Termin im Juni steht, aber die Abnahme durch den Sachverstaendigen ist noch "
     "nicht angefragt, und der hat erfahrungsgemaess sechs bis acht Wochen Vorlauf. "
     "Wir waren uns einig, dass der Termin jetzt angefragt wird, auch wenn er "
     "moeglicherweise nochmal verschoben werden muss. Der Bauleiter macht das. "
     "Vierter Punkt, kurz angerissen, war die Frage der Dokumentation. Der Bauherr "
     "moechte die Revisionsunterlagen digital und strukturiert, nicht als Stapel PDFs. "
     "Das ist bei uns bisher nicht der Standard, das muss ich intern klaeren. "
     "Naechster Jour fixe ist in zwei Wochen, gleicher Ort, gleiche Zeit."),

    ("Kundengespraech schwierig",
     "Notiz zum Gespraech mit Frau Doerner vom Objekt Lindenstrasse, heute Nachmittag, "
     "etwa eine Dreiviertelstunde, telefonisch. Das Gespraech war schwierig, "
     "ich halte es deshalb ausfuehrlicher fest. Ausgangspunkt war ihre Beschwerde, "
     "dass die Trocknungsgeraete seit ueber zwei Wochen laufen und niemand ihr sagen "
     "kann, wie lange es noch dauert. Das ist berechtigt, wir haben tatsaechlich seit "
     "der letzten Messung nicht mehr aktiv informiert. Ich habe mich dafuer entschuldigt "
     "und zugesagt, dass sie ab sofort woechentlich eine kurze Rueckmeldung bekommt, "
     "auch wenn sich nichts geaendert hat. Zweiter Punkt war die Kuechennutzung. "
     "Sie besteht darauf, dass ihr eine Ersatzkueche gestellt wird, und verweist darauf, "
     "dass die Hausverwaltung das angeblich zugesagt hat. Mir gegenueber ist das nicht "
     "bestaetigt, und es ist auch nicht unsere Zusage. Ich habe ihr gesagt, dass ich "
     "das mit der Hausverwaltung klaere und mich bis Freitag melde. Wichtig: ich habe "
     "ausdruecklich nichts zugesagt, was Kosten ausloest. Dritter Punkt war die Frage "
     "der Mietminderung. Da habe ich sie an die Hausverwaltung verwiesen, das ist nicht "
     "unser Thema und ich moechte mich dazu auch nicht aeussern. Sie hat das akzeptiert. "
     "Abschliessend hat sie gefragt, ob die Feuchtigkeit gesundheitlich bedenklich ist. "
     "Ich habe gesagt, dass die Messwerte im Bereich liegen, den wir bei einem solchen "
     "Schaden erwarten, dass ich aber keine gesundheitliche Bewertung abgeben kann und "
     "werde. Falls sie das moechte, muss sie einen Sachverstaendigen einschalten. "
     "Mein Eindruck: sie fuehlt sich vor allem uebergangen, weniger dass sie uns "
     "wirklich etwas vorwirft. Wenn wir die woechentliche Rueckmeldung einhalten, "
     "beruhigt sich das. Bitte Termin dafuer fest einplanen, sonst geht es unter."),

    ("Technische Klaerung",
     "Technische Notiz zur Klaerung der Heizungsverteilung im Bauteil C, "
     "aufgenommen nach dem Termin mit dem Planungsbuero. "
     "Ausgangslage ist, dass die bestehende Verteilung aus den neunziger Jahren stammt "
     "und fuer die geplante zusaetzliche Last nicht ausgelegt ist. Es gibt drei "
     "Moeglichkeiten. Erstens, die Verteilung komplett erneuern. Das ist sauber, "
     "aber teuer und braucht eine laengere Abschaltung, was im laufenden Betrieb "
     "schwierig ist. Zweitens, eine zusaetzliche Verteilung parallel setzen und nur "
     "die neuen Kreise darauf legen. Das geht im laufenden Betrieb, erzeugt aber "
     "eine hydraulisch unschoene Situation mit zwei Verteilern, und die Regelung "
     "wird komplizierter. Drittens, die bestehende Verteilung ertuechtigen, also "
     "einzelne Komponenten tauschen und den Rest belassen. Das ist die billigste "
     "Variante, aber wir uebernehmen damit Verantwortung fuer Bauteile, deren Zustand "
     "wir nicht vollstaendig kennen. Meine Empfehlung ist Variante zwei, weil sie "
     "den Betrieb am wenigsten stoert und technisch beherrschbar ist, wenn wir die "
     "Regelung von vornherein richtig planen. Variante drei wuerde ich nur machen, "
     "wenn wir vorher eine vollstaendige Zustandspruefung durchfuehren duerfen, "
     "und dafuer wird der Kunde die Kosten vermutlich nicht tragen wollen. "
     "Das muss ins Angebot als Variantenvergleich, mit klarer Empfehlung und mit "
     "dem Hinweis, welche Risiken bei Variante drei bei uns haengen bleiben."),

    # --- sehr lang: ausufernde Sprachnachricht (~5-6 min) ---
    ("Wochenrueckblick lang",
     "So, ausfuehrlicher Wochenrueckblick, ich diktiere das mal komplett, "
     "damit es nicht wieder untergeht. Fangen wir mit Nordring an. "
     "Der Rohbau ist durch und abgenommen, das Protokoll liegt vor, es gibt neun "
     "Restpunkte, davon sind sieben Kleinigkeiten und zwei muessen wir ernst nehmen: "
     "die Durchfuehrung im Technikraum ist nicht fachgerecht abgeschottet, und im "
     "Treppenhaus fehlt noch das Gelaender, das haengt seit drei Wochen am Schlosser. "
     "Beim Gelaender muessen wir Druck machen, sonst haben wir ein Problem mit der "
     "Verkehrssicherung, und das faellt am Ende auf uns zurueck. "
     "Die Gebaeudetechnik liegt etwa eine Woche hinter dem Plan, Grund sind die "
     "verspaeteten Lueftungskanaele. Aufholen geht, wenn die Elektromontage parallel "
     "laeuft, aber das muss der Bauleiter koordinieren, und er hat im Moment sehr viel "
     "auf dem Tisch. Ich wuerde ihm anbieten, dass wir die Koordination der "
     "Gewerkeschnittstelle uebernehmen, das kostet uns zwei, drei Stunden die Woche, "
     "spart aber vermutlich mehr. Kosten liegen drei Prozent ueber Ansatz, Mehrmengen "
     "Estrich und die Brandschotts. Die Gegenueberstellung mache ich bis Mittwoch. "
     "Zweites Thema Lindenstrasse. Der Wasserschaden ist in der Trocknung, "
     "Frau Doerner war unzufrieden, wir haben jetzt eine woechentliche Rueckmeldung "
     "vereinbart, die muss aber auch wirklich stattfinden, sonst ist es schlimmer als "
     "vorher. Die Frage der Ersatzkueche liegt bei der Hausverwaltung, da halte ich "
     "uns raus, ausser dass ich nachfasse. Die Versicherung ist inzwischen informiert, "
     "die Schadensnummer liegt im Ordner. Wichtig ist der Zwischenzaehler fuer die "
     "Trocknungsgeraete, das hatten wir beim letzten Mal vergessen und uns dann um "
     "vierhundert Euro gestritten, das muss diesmal dokumentiert sein. "
     "Drittes Thema Bergmann, Lueftung Bauteil C. Angebot mit zwei Varianten bis Ende "
     "naechster Woche, Budget etwa achtzigtausend, Variante mit Waermerueckgewinnung "
     "liegt vermutlich darueber, das muss ehrlich dargestellt werden. "
     "Die Aufstellung der nicht zugaenglichen Bereiche braucht er vorab fuer den "
     "Betriebsrat, das ist ein kleiner Aufwand, aber terminkritisch fuer ihn. "
     "Elektrik mit Neele abstimmen. Viertes Thema, intern: die Pruefprotokolle vom "
     "letzten Monat sind immer noch nicht abgelegt. Das ist inzwischen das dritte Mal, "
     "dass ich das diktiere. Wir brauchen dafuer eine feste Zustaendigkeit, sonst "
     "passiert es nicht. Ich wuerde vorschlagen, dass wir das im naechsten Bueromeeting "
     "verbindlich verteilen. Fuenftes Thema, auch intern: die Anfrage wegen der "
     "digitalen Revisionsunterlagen bei Nordring. Der Bauherr moechte das strukturiert "
     "und nicht als PDF-Stapel. Das koennen wir aktuell nicht sauber liefern. "
     "Entweder wir sagen ehrlich, dass wir es nicht koennen, oder wir investieren "
     "einmal Zeit und bauen uns eine Struktur, die wir dann bei allen Projekten nutzen. "
     "Ich tendiere klar zum Zweiten, weil die Anforderung oefter kommen wird. "
     "Das sollten wir aber bewusst entscheiden und nicht nebenher. "
     "Letzter Punkt: naechste Woche bin ich Donnerstag und Freitag nicht da, "
     "Fortbildung. Termine bitte entsprechend legen."),

    ("Uebergabe Urlaubsvertretung",
     "Uebergabe fuer die Urlaubsvertretung, ich bin ab Montag zwei Wochen weg. "
     "Ich gehe die laufenden Sachen der Reihe nach durch. "
     "Erstens Nordring. Ansprechpartner ist der Bauleiter, Nummer steht im Verteiler. "
     "Offen ist die Gegenueberstellung der Kosten, die habe ich bis Freitag fertig und "
     "verschickt, da muss niemand ran. Offen ist das Gelaender im Treppenhaus, "
     "da bitte woechentlich beim Schlosser nachfassen, der reagiert nur, wenn man "
     "hartnaeckig ist. Falls die Lueftungskanaele kommen, bitte pruefen lassen, ob die "
     "Abmessungen stimmen, da hatten wir bei der letzten Lieferung eine Abweichung. "
     "Zweitens Lindenstrasse. Die Trocknung laeuft, Messung ist immer montags. "
     "Ganz wichtig: Frau Doerner bekommt jeden Freitag eine kurze Rueckmeldung, "
     "auch wenn es nichts Neues gibt. Das haben wir ihr zugesagt, und wenn das ausfaellt, "
     "haben wir ein echtes Problem. Die Frage der Ersatzkueche liegt bei der "
     "Hausverwaltung, da bitte nur nachfassen, nichts zusagen, was Geld kostet. "
     "Drittens Bergmann. Das Angebot ist raus, wenn eine Rueckfrage kommt, bitte "
     "sammeln und mir per Mail schicken, ich schaue einmal in der Woche rein. "
     "Nichts verhandeln, der Vorgang ist heikel wegen des Budgets. "
     "Viertens allgemein: die Bestellungen laufen normal weiter, Freigabegrenze ist "
     "wie immer tausend Euro, darueber bitte mit der Geschaeftsfuehrung abstimmen. "
     "Fuenftens, falls ein neuer Schadensfall reinkommt: Aufnahme machen, Fotos, "
     "Messung, aber noch keine Zusagen zur Dauer. Das ist der Fehler, den wir bei "
     "Lindenstrasse gemacht haben. Ich bin im Notfall erreichbar, aber bitte wirklich "
     "nur im Notfall. Alles andere sammeln, ich arbeite es nach der Rueckkehr ab."),

    ("Ortstermin ausfuehrlich",
     "Ortstermin Objekt Ahornweg, Bestandsaufnahme fuer die geplante Modernisierung. "
     "Ich diktiere das direkt vor Ort, deshalb etwas ungeordnet. "
     "Das Gebaeude ist von neunzehnhundertdreiundsiebzig, sechs Wohneinheiten, "
     "drei Vollgeschosse plus ausgebautes Dachgeschoss. Der Allgemeinzustand ist "
     "fuer das Alter ordentlich, aber es ist offensichtlich seit den Neunzigern "
     "nichts Grundlegendes gemacht worden. Die Heizungsanlage ist ein Gaskessel "
     "aus zweitausendzwei, laeuft noch, ist aber am Ende der ueblichen Nutzungsdauer. "
     "Die Verteilung im Keller ist ungedaemmt, das ist ein einfacher Hebel, "
     "den sollten wir auf jeden Fall aufnehmen. Die Fenster sind zweifachverglast, "
     "Baujahr vermutlich neunziger Jahre, Beschlaege teilweise ausgeschlagen. "
     "Tausch waere sinnvoll, ist aber ein grosser Posten, das muss der Eigentuemer "
     "entscheiden. Die Elektroinstallation ist der kritische Punkt. Im Keller ist "
     "noch eine alte Verteilung mit Schraubsicherungen, in zwei Wohnungen sind "
     "die Leitungen nach Aussage des Eigentuemers nie erneuert worden. "
     "Das muessen wir uns genauer ansehen, da gehe ich von Erneuerungsbedarf aus, "
     "und das ist im bewohnten Zustand aufwendig. Das Dach ist von aussen unauffaellig, "
     "im Spitzboden habe ich keine Feuchtespuren gesehen. Die Daemmung der obersten "
     "Geschossdecke fehlt, das ist wieder ein einfacher Hebel. "
     "Am Balkon auf der Suedseite im ersten Obergeschoss sind Risse in der Bruestung, "
     "das sollte ein Statiker anschauen, bevor wir dazu irgendetwas sagen. "
     "Der Eigentuemer moechte in Stufen vorgehen und hat als Budget fuer den ersten "
     "Schritt etwa sechzigtausend genannt. Damit kaeme man an Heizungsdaemmung, "
     "oberste Geschossdecke und die Elektroverteilung im Keller, das waere aus meiner "
     "Sicht auch die richtige Reihenfolge, weil es Sicherheit und Verbrauch trifft "
     "und die Bewohner am wenigsten stoert. Fenster und Balkon waeren Stufe zwei. "
     "Ich schlage vor, wir machen daraus ein kurzes Stufenkonzept mit groben Kosten "
     "je Stufe, keine Detailplanung. Termin fuer die Elektropruefung muss mit den "
     "Mietern abgestimmt werden, das dauert erfahrungsgemaess."),

    ("Fehlersuche Anlage",
     "Notiz zur Stoerungssuche an der Lueftungsanlage im Verwaltungsgebaeude, "
     "Dienstag, etwa drei Stunden vor Ort. Gemeldet war, dass es in den Bueros "
     "im zweiten Obergeschoss zieht und gleichzeitig im Besprechungsraum stickig ist. "
     "Erste Vermutung war ein Abgleichproblem. Ich habe die Volumenstroeme an sechs "
     "Auslaessen gemessen, im zweiten Obergeschoss liegen die Werte deutlich ueber "
     "Auslegung, im Besprechungsraum etwa bei der Haelfte. Das passt zum Bild. "
     "Die Ursache ist aber nicht der Abgleich allein. Beim Blick in die Anlage ist "
     "aufgefallen, dass die Volumenstromregler im Strang Ost auf einen festen Wert "
     "gestellt sind statt auf Regelung, offenbar seit einer Wartung im letzten Jahr. "
     "Wer das gemacht hat, ist nicht dokumentiert, im Wartungsprotokoll steht dazu "
     "nichts. Zweiter Punkt: der Filter der Zuluft ist stark zugesetzt, "
     "Differenzdruck deutlich ueber dem Grenzwert. Das erklaert, warum die Anlage "
     "insgesamt am Limit faehrt. Filterwechsel ist ohnehin faellig. "
     "Dritter Punkt, und der ist unangenehm: die Brandschutzklappe im Strang Ost "
     "hat beim Funktionstest nicht sauber geschlossen. Das ist ein sicherheitsrelevanter "
     "Mangel, das muss schriftlich an den Betreiber und zwar zeitnah. "
     "Mein Vorschlag: Filterwechsel und Regler auf Regelung zurueckstellen koennen wir "
     "kurzfristig machen, das loest das Komfortproblem vermutlich zu achtzig Prozent. "
     "Die Brandschutzklappe braucht eine separate Beauftragung, das ist kein Nebenbei. "
     "Und wir sollten dem Betreiber empfehlen, den Abgleich einmal komplett "
     "nachfahren zu lassen, weil ich nicht ausschliessen kann, dass in anderen "
     "Straengen aehnliche Eingriffe gemacht wurden, die niemand dokumentiert hat."),
]
