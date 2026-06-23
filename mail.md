 Ich habe einen MCP-Server für ControlDesk gebaut – ein LLM kann damit direkt ControlDesk steuern: Experimente anlegen, Layouts erstellen, Instrumente platzieren. Implementiert in .NET, auf GitHub mit einer Action, die automatisch eine .exe baut.

Der Aufwand: 30 Minuten Prompt ausarbeiten, 1 Stunde Implementierung durch Claude Opus, ca. 12 Dollar. Kein Nachprompting. Das Ergebnis war ein vollständiger MCP-Server inklusive Integration Tests.

Wir tun uns bei dSPACE noch schwer mit LLMs, Agents und dem ganzen Thema. Dabei zeigt dieses Beispiel, wie wenig Aufwand es braucht. Welche Technologie man dafür nutzt, ist völlig zweitrangig. Die Frage ist, wie wir die Leute befähigen, diesen ersten Schritt zu machen.

Das GitHub-Repository findet ihr hier: [TODO: Link einfügen]

Was der MCP-Server kann:

- create_experiment_with_project – Legt ein neues ControlDesk-Projekt zusammen mit einem Experiment an und aktiviert es. Parameter: projectName (Pflicht), experimentName (Pflicht), projectRoot (optional, Pfad zum Projektverzeichnis).
- create_layout – Erstellt ein neues, leeres Layout im aktiven Experiment. Parameter: layoutName (Pflicht).
- list_layouts – Listet alle Layouts im aktiven Experiment mit der jeweiligen Anzahl an Instrumenten. Keine Parameter.
- create_instrument – Fügt ein Instrument zu einem Layout hinzu. Als Typ sind alle ControlDesk-Instrumentbibliotheken möglich, z. B. "Variable Array", "Time Plotter" oder "Knob". Parameter: layoutName, instrumentType, instrumentName (alle Pflicht), x, y, width, height (optional, mit Standardwerten).
- list_layout_instruments – Listet alle Instrumente eines Layouts inkl. ihrer Positionen. Parameter: layoutName (Pflicht).
- inspect_instrument – Gibt Details zu einem einzelnen Instrument zurück (Name, Position, Größe, Daten). Parameter: layoutName, instrumentName (beide Pflicht).
- delete_project – Löscht ein ControlDesk-Projekt inklusive aller Daten von der Festplatte. Parameter: projectName (Pflicht), projectRoot (optional).
