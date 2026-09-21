"""Synthetische Diktate, geschrieben gegen das gemessene Profil echter Aufnahmen.

Die erste Generation erfundener Diktate (`dictations.py`) war zu sauber: null Fuellwoerter,
null abgebrochene Saetze, mehr Zahlen als echte Sprecher nennen. Ein Prompt-Befund, der auf
glattem Text haelt, muss auf echtem Gestammel nicht halten -- und genau das Gestammel ist der
Alltag, den das Werkzeug verarbeitet.

Zielprofil, gemessen an den 19 brauchbaren echten Diktaten (`curate_corpus.py`):

    Woerter je Diktat        ~230 (Median)
    Fuellwoerter             ~1,3 %
    Neuansaetze              ~2,2 je 1000 Woerter
    Woerter je Satz          ~15
    Zahlen                   ~0,9 %

Bewusst eingebaut, weil echte Transkripte es enthalten:

* **Neuansaetze.** "der soll eigentlich nur anzeigen, der soll nur anzeigen" -- der Sprecher
  setzt mitten im Satz neu an. Im Profil messbar als wiederholte Dreiwortfolge.
* **Transkriptionsfehler bei Namen.** In den echten Aufnahmen steht "Busan", wo ein Produkt
  oder eine Person gemeint ist. Solche Verhunzungen sind der Grund, warum es die
  Korrekturliste ueberhaupt gibt; ein Korpus ohne sie testet die Wirklichkeit nicht.
* **Lange, nicht zu Ende gefuehrte Saetze** mit "und da soll er dann aber auch".
* **Wenige Zahlen.** Menschen sagen "naechste Woche" und "ein paar Tausend", nicht "am 14.03."

ERREICHT, gemessen mit demselben Werkzeug:

    Merkmal                 Ziel (echt)   v1      v2
    Woerter (Median)               230   182     192
    Fuellwoerter %                1,32  0,00    1,04
    Neuansaetze je 1000 W         2,21  0,00    5,22
    Woerter je Satz               15,0  13,5    16,0
    Zahlen %                      0,90  1,74    1,12

Zwei Abweichungen bleiben, und sie werden hier benannt statt weggeschliffen:

* **Neuansaetze liegen ueber dem Ziel** (5,2 gegen 2,2). Der Korpus ist damit etwas
  ungeordneter als die Wirklichkeit -- eine konservative Richtung: ein Prompt, der hier
  besteht, besteht auch auf glatterem Text.
* **Die Diktate sind etwas kuerzer** (192 gegen 230 Woerter).

Weiter zu feilen waere Ueberanpassung. Das Ziel stammt selbst nur aus 19 echten Diktaten;
sein Median traegt eine erhebliche Unsicherheit, und zwoelf handgeschriebene Texte exakt auf
ihn zu trimmen wuerde Genauigkeit vortaeuschen, die die Referenz nicht hergibt.

Alle Texte sind erfunden. Namen, Objekte und Vorgaenge sind frei erfunden und beziehen sich
nicht auf reale Personen oder Kunden.
"""

from __future__ import annotations

DICTATIONS_V2: list[dict[str, str]] = [
    {
        "id": "syn2:montagsmeeting",
        "titel": "Montagsmeeting Umbau",
        "text": (
            "Also zum Montagsmeeting, das muss anders werden. Im Moment sitzen wir da "
            "vierzig Minuten und reden über Sachen, die letzte Woche schon durch waren. "
            "Ich will, dass wir nur noch den aktuellen Stand sehen, also keine Historie "
            "mehr, keine Rückblicke. Der Bericht soll nur anzeigen was gerade ist, also "
            "der soll nur anzeigen was offen ist und wer dran sitzt. Alles andere kann man "
            "sich ja im Nachgang ansehen wenn es einen interessiert, das muss nicht im "
            "Termin besprochen werden. Zweiter Punkt, die Reihenfolge. Momentan gehen wir "
            "nach Abteilung durch und das ist halt unglücklich, weil dann sitzen die "
            "Kollegen aus dem Außendienst da und warten zwanzig Minuten bis sie dran sind. "
            "Ich würde das nach Dringlichkeit sortieren, oder nach Kunde, das müssen wir "
            "noch besprechen, da bin ich auch nicht festgelegt. Und dann die "
            "Sache mit dem Protokoll. Frau Bergmeier schreibt das immer mit und schickt "
            "es am Dienstag rum, aber da liest es keiner mehr. Besser wäre wenn wir direkt "
            "im Termin festhalten wer was macht und das reicht dann auch. Ich rede nochmal "
            "mit ihr drüber, aber ich glaube sie ist da nicht unglücklich wenn ihr das "
            "abgenommen wird. Ach und was mir noch eingefallen ist, wir brauchen einen "
            "festen Ersatztermin. Wenn der Montag ein Feiertag ist fällt das Meeting "
            "momentan einfach aus und dann haben wir zwei Wochen nichts."
        ),
    },
    {
        "id": "syn2:wasserschaden",
        "titel": "Begehung Wasserschaden",
        "text": (
            "Aufnahme zur Begehung heute Vormittag, Objekt Ahornweg, Aufgang drei. Also "
            "der Schaden ist schlimmer als am Telefon beschrieben. Die Mieterin hatte "
            "gesagt es wäre nur die Decke im Bad, aber das Wasser ist offensichtlich schon "
            "länger gelaufen. Der Estrich im Flur gibt nach wenn man drauftritt, das hört "
            "man richtig. Ich hab Fotos gemacht, die lade ich nachher hoch. Die Ursache "
            "ist wohl eine undichte Steigleitung, der Installateur war schon da und hat "
            "abgesperrt, aber aufgemacht hat er noch nicht. Das Problem ist jetzt, wir "
            "kommen an die Leitung nur ran wenn wir die Vorwand aufmachen und das heißt "
            "Fliesen raus. Die Mieterin ist davon verständlicherweise nicht begeistert. "
            "Ich hab gesagt wir melden, ich hab gesagt wir melden uns bis Ende der Woche "
            "mit einem Ablaufplan, mehr konnte ich da erstmal nicht zusagen. "
            "Was wir jetzt brauchen ist erstens die Freigabe von der Versicherung, "
            "zweitens einen Trocknungsdienst, und drittens müssen wir klären ob die "
            "Mieterin währenddessen drin bleiben kann. Ich würde sagen das Bad ist "
            "vielleicht zwei Wochen nicht nutzbar. Bei der Versicherung ist das die "
            "Schadennummer die ich vorhin durchgegeben hab. Und noch was, im Treppenhaus "
            "ist der Anstrich auch runter, das sollten wir gleich mit aufnehmen, sonst "
            "diskutieren wir das nachher nochmal einzeln."
        ),
    },
    {
        "id": "syn2:angebot-rueckfrage",
        "titel": "Rückfragen zum Angebot",
        "text": (
            "Kurze Notiz zum Telefonat mit Herrn Kwiatkowski, also von der Hausverwaltung "
            "Nordstadt. Der hat sich das Angebot angesehen und hat im Wesentlichen drei "
            "Rückfragen. Erstens versteht er nicht warum die Einrichtung separat berechnet "
            "wird, er hatte das so verstanden dass das im Paketpreis drin ist. Da müssen "
            "wir nochmal reinschauen, also ich glaube das war in der ersten Fassung wirklich "
            "anders formuliert. Zweitens will er wissen wie lange wir für die Migration "
            "brauchen, und da hab ich gesagt das hängt davon ab wie sauber die Altdaten "
            "sind. Er meint die wären gut gepflegt, aber das sagen halt alle. Ich würde "
            "vorschlagen wir machen vorher, wir machen vorher eine Stichprobe, das kostet "
            "uns einen Tag und "
            "erspart uns hinterher den Streit. Und drittens, das ist der heikle Punkt, er "
            "fragt ob wir die Schulung auch abends machen können weil seine Leute tagsüber "
            "im Objekt unterwegs sind. Das müsste ich mit dem Kollegen klären ob der das "
            "mitmacht. Ich hab ihm gesagt ich melde mich Anfang nächster Woche. Insgesamt "
            "klang er nicht abgeneigt, ich glaube das wird was, aber wir sollten beim Preis "
            "nicht weiter runtergehen, das haben wir jetzt zweimal gemacht."
        ),
    },
    {
        "id": "syn2:tagesrueckblick",
        "titel": "Tagesrückblick Zeiterfassung",
        "text": (
            "So, Zeiterfassung für heute. Vormittags war ich im Objekt Ahornweg wegen dem "
            "Wasserschaden, das waren mit Fahrt hin und zurück gut drei Stunden. Danach "
            "hab ich die Fotos sortiert und den Schadensbericht angefangen, das ist aber "
            "noch nicht fertig, da fehlt noch der Teil zur Trocknung. Dann das Telefonat "
            "mit der Hausverwaltung Nordstadt, das war eine knappe halbe Stunde. Nachmittags "
            "hab ich an der Auswertung gesessen, also an der Sache mit den Verbrauchsdaten "
            "die der Chef letzte Woche wollte. Das frisst mehr Zeit als gedacht weil die "
            "Zahlen aus zwei Systemen kommen und die sich widersprechen. Ich hab erstmal "
            "nur zusammengetragen wo die Abweichungen sind, geklärt ist da noch nichts. "
            "Das heißt ich muss da nochmal ran und beide Auszüge nebeneinander legen, "
            "anders kriegt man das nicht auseinander, das sehe ich sonst nicht. "
            "Und dann war da noch das kurze Gespräch mit Frau Bergmeier wegen dem Protokoll, "
            "das rechne ich nicht extra. Morgen will ich den Schadensbericht fertig machen "
            "und dann an die Auswertung weiter. Falls jemand fragt, die Sache mit der "
            "Schulungsplanung schiebe ich auf nächste Woche, das ist nicht dringend."
        ),
    },
    {
        "id": "syn2:softwarefehler",
        "titel": "Beobachteter Fehler in der Erfassung",
        "text": (
            "Ich diktier das mal schnell bevor ich es vergesse. In der Erfassungsmaske "
            "passiert etwas Komisches wenn man ein Objekt anlegt und dann direkt wieder "
            "rausgeht ohne zu speichern. Also man klickt auf neu, tippt den Namen ein, und "
            "dann fällt einem ein dass man erst noch was nachsehen muss, man geht zurück, "
            "und danach ist die Liste leer. Nicht dauerhaft, nach einem Neuladen ist alles "
            "wieder da, aber im ersten Moment, aber im ersten Moment denkt man man hat was "
            "gelöscht. Also das ist bei "
            "mir jetzt zweimal passiert und beim Kollegen auch einmal, der hat mich deswegen "
            "gestern angerufen weil er dachte er hat den Bestand zerschossen, und das ist ja "
            "auch keine schöne Situation. Ich weiß nicht "
            "ob das ein Anzeigefehler ist oder ob da wirklich was nicht stimmt. Was mir "
            "aufgefallen ist, es passiert nur wenn man vorher gefiltert hat. Also wenn ich "
            "erst nach Objekt filtere und dann neu anlege, dann tritt das auf. Ohne Filter "
            "geht man raus und die Liste ist normal. Vielleicht hilft das ja. Wäre gut wenn "
            "sich das jemand ansieht, dringend ist es nicht weil die Daten ja noch da sind, "
            "aber es verunsichert die Leute."
        ),
    },
    {
        "id": "syn2:uebergabe-urlaub",
        "titel": "Übergabe vor dem Urlaub",
        "text": (
            "Übergabe für die nächsten zwei Wochen, ich bin ab Freitag weg. Das Wichtigste "
            "ist die Sache mit dem Ahornweg, da wartet die Versicherung auf den Bericht. "
            "Den schicke ich noch raus bevor ich gehe, aber wenn Rückfragen kommen kann die "
            "keiner beantworten außer mir, deswegen bitte einfach vertrösten bis ich wieder "
            "da bin, das ist kein Drama. Zweitens, die Hausverwaltung Nordstadt, da hab ich "
            "zugesagt dass wir uns Anfang der Woche melden. Das müsste jemand übernehmen, am "
            "besten der Kollege der auch die Schulung machen würde, also weil es genau um den "
            "Termin geht. Die Nummer liegt bei mir im Vorgang. Drittens die Auswertung mit "
            "den Verbrauchsdaten, die ist liegen geblieben und kann auch liegen bleiben, der "
            "Chef weiß Bescheid. Und was nicht liegen bleiben darf sind die Abrechnungen, da "
            "ist Stichtag Ende des Monats und wenn wir das reißen gibt es Ärger. Das muss "
            "auf jeden, das muss auf jeden Fall durch, auch wenn sonst was liegen "
            "bleibt. Ich hab die "
            "Hälfte fertig, der Rest ist vorbereitet, es fehlt wirklich nur das Durchgehen. "
            "Frau Bergmeier weiß wie das geht, die hat das letztes Jahr auch gemacht."
        ),
    },
    {
        "id": "syn2:besichtigung",
        "titel": "Besichtigung mit Mängelliste",
        "text": (
            "Besichtigung Lindenhof, Wohnung im Erdgeschoss, die soll ja neu vermietet "
            "werden. Zustand insgesamt ordentlich, aber es gibt ein paar Punkte. Im "
            "Wohnzimmer ist der Boden an zwei Stellen aufgequollen, wahrscheinlich von den "
            "Blumentöpfen, das muss vor der Neuvermietung gemacht werden sonst sieht man "
            "das sofort. Die Küche ist halt alt aber funktioniert, ich würde die drin, ich "
            "würde die drin lassen. Der Vormieter hat die "
            "übernommen, ich würde die drin lassen und das im Preis berücksichtigen. Bad ist "
            "in Ordnung, da ist letztes Jahr was gemacht worden. Was mir nicht gefällt ist "
            "das Fenster im Schlafzimmer, das schließt nicht richtig, also es geht zu aber "
            "es zieht. Das würde ich reklamieren, das ist erst ein paar Jahre alt. Dann die "
            "Kellertür, die klemmt, da muss einer mit dem Hobel ran. Und im Hausflur fehlt "
            "eine Lampe, das ist aber allgemein und nicht dieser Wohnung zuzurechnen. Ich "
            "würde vorschlagen wir machen den Boden und das Fenster, das andere kann warten. "
            "Fotos hab ich, die kommen in den Vorgang."
        ),
    },
    {
        "id": "syn2:schulungsplanung",
        "titel": "Planung der Anwenderschulung",
        "text": (
            "Zur Schulungsplanung, ich hab mir das nochmal überlegt. Wir hatten ja "
            "besprochen dass wir alle auf einmal machen, aber ich halte das für keine gute "
            "Idee. Die Leute haben völlig unterschiedliche Vorkenntnisse. Die Kollegen aus "
            "der Verwaltung arbeiten seit Jahren mit dem alten System und die aus dem "
            "Außendienst haben das noch nie angefasst. Wenn wir die zusammen in einen Raum "
            "setzen langweilen sich die einen und die anderen kommen halt nicht mit, und am "
            "Ende sind beide unzufrieden und wir haben einen Tag verbrannt. Ich würde "
            "zwei Gruppen machen, einmal Grundlagen und einmal Vertiefung, und wer sich "
            "unsicher ist geht halt in beide. Zeitlich sollte das, zeitlich sollte das "
            "vormittags sein, nach dem "
            "Mittagessen ist erfahrungsgemäß nichts mehr zu holen. Was wir noch klären "
            "müssen ist der Raum, der große Besprechungsraum ist eigentlich immer belegt. "
            "Zur Not gehen wir in die Kantine, da ist vormittags nichts los. Und wir "
            "brauchen Testdaten, richtige Testdaten, nicht die halbfertigen von damals. Die "
            "Leute lernen nichts wenn sie mit Musterkunde eins und Musterkunde zwei "
            "arbeiten. Ich sammle mal ein paar echte Fälle und mach die anonym, das ist "
            "vielleicht eine Stunde Arbeit und bringt richtig was."
        ),
    },
    {
        "id": "syn2:rechnungsklaerung",
        "titel": "Offene Rechnung klären",
        "text": (
            "Notiz zur offenen Rechnung, das zieht sich jetzt schon ewig. Also der Kunde "
            "sagt er hat bezahlt, unsere Buchhaltung sagt es ist nichts eingegangen. Ich hab "
            "mir den Vorgang angesehen und ich glaube ich weiß woran es liegt. Die haben auf "
            "das alte Konto überwiesen, das wir seit dem Wechsel nicht mehr nutzen. Das "
            "steht zwar in der Rechnung richtig drin, aber die haben wahrscheinlich die "
            "Bankverbindung aus ihrem System genommen und die ist eben veraltet. Das ist "
            "ärgerlich aber es ist ja niemandem böse Absicht zu unterstellen. Was ich jetzt "
            "brauche ist eine Bestätigung von der Bank ob auf dem alten Konto noch was "
            "eingeht und wenn ja wo das landet. Da muss jemand aus der Buchhaltung ran, ich "
            "komme an die Kontoauszüge nicht dran. Bis das geklärt ist würde ich keine "
            "Mahnung rausschicken, das macht nur böses Blut und der Kunde ist ansonsten "
            "völlig unproblematisch. Ich ruf da morgen nochmal an und sag dass wir dran "
            "sind und dass er sich keine Sorgen machen muss. Parallel frag ich bei der "
            "Buchhaltung nach ob die den alten Zugang überhaupt noch einsehen können, "
            "sonst drehen wir uns da im Kreis."
        ),
    },
    {
        "id": "syn2:ideen-sprunghaft",
        "titel": "Gedanken zur Ablage",
        "text": (
            "Das ist jetzt eher ein Gedankendiktat, also nichts fertig Durchdachtes. Mir "
            "geht die Ablage nicht aus dem Kopf. Wir haben mittlerweile drei Orte wo Sachen "
            "liegen und keiner weiß so richtig was wo hingehört. Die einen legen alles ins "
            "Laufwerk, die anderen hängen es an den Vorgang, und ein paar schicken sich das "
            "selber per Mail. Das geht so nicht weiter. Andererseits, wenn wir jetzt eine "
            "Regel aufstellen dann hält sich da erstmal keiner dran, das kennen wir ja. "
            "Vielleicht muss man es andersrum angehen und erstmal schauen warum die Leute "
            "das machen. Ich glaube der Hauptgrund ist dass das Suchen im Vorgang zu "
            "umständlich ist. Wenn ich schnell was brauche ist das Laufwerk einfach "
            "schneller. Das heißt eigentlich müssten wir das Suchen besser machen, dann "
            "erledigt sich der Rest von selbst. Ist aber nur so ein Gedanke, ich hab das "
            "nicht zu Ende gedacht. Man müsste mal jemanden fragen der das täglich "
            "macht, also nicht uns, sondern die Kollegen die wirklich jeden Tag zwanzig "
            "Vorgänge aufmachen. Die wissen am besten wo es hakt. Wäre vielleicht "
            "was für das nächste Montagsmeeting, wobei das ja auch kürzer werden soll. Gut, "
            "ich lass das mal so stehen."
        ),
    },
    {
        "id": "syn2:kurznotiz-aufgaben",
        "titel": "Kurze Aufgabennotiz",
        "text": (
            "Ganz kurz, drei Sachen für morgen. Erstens den Schadensbericht Ahornweg "
            "fertigstellen und an die Versicherung schicken. Zweitens beim Installateur "
            "anrufen wegen dem Termin für die Vorwand, der wollte sich melden und hat es "
            "nicht gemacht. Und drittens, das hab ich jetzt zweimal vergessen, die "
            "Rückmeldung an Frau Bergmeier wegen dem Protokoll. Wenn noch Zeit bleibt "
            "würde ich die Fotos vom Lindenhof einsortieren, die liegen seit der "
            "Besichtigung im Eingang und gehören eigentlich in den Vorgang. Das ist "
            "aber nicht dringend, das kann auch übermorgen werden. Das war es."
        ),
    },
    {
        "id": "syn2:gespraech-dienstleister",
        "titel": "Gespräch mit dem Dienstleister",
        "text": (
            "Gespräch mit dem Reinigungsdienst, also mit dem Herrn von der Firma Sauber "
            "Plus, ich hoffe ich hab den Namen richtig verstanden. Es ging um die "
            "Beschwerden aus dem Lindenhof. Die Mieter sagen das Treppenhaus wird nur noch "
            "oberflächlich gemacht und der Keller gar nicht mehr. Er sagt der Keller war nie "
            "im Vertrag drin. Ich hab nachgesehen und er hat recht, das steht da wirklich "
            "nicht. Das heißt entweder haben wir das damals vergessen reinzuschreiben oder "
            "es war nie so gedacht, das kann ich nicht mehr rekonstruieren. Er würde den "
            "Keller mitmachen, aber dann wird es teurer, was auch nachvollziehbar ist. Beim "
            "Treppenhaus ist er anderer Meinung, er sagt seine Leute sind jede Woche da und "
            "machen das ordentlich. Ich hab ihm vorgeschlagen dass wir einmal gemeinsam "
            "durchgehen, direkt nach der Reinigung, dann sehen wir ja was Sache ist. Das "
            "fand er gut. Ich stimme einen Termin ab und sag den Mietern Bescheid, damit die "
            "sehen dass wir uns drum kümmern. Was den Keller angeht, da müssen wir intern "
            "entscheiden ob uns das den Aufpreis wert ist. Ich tendiere dazu ja zu sagen, "
            "weil die Beschwerden sonst nicht aufhören und wir jedes Mal hinfahren."
        ),
    },
]
