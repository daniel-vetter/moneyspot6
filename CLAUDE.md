* Bei neuen Migrationen kannst du den Down Step immer entfernen. Wir brauchen nur Up.
* Für neue ef core migrationen schreibe die in normaler schreibweise, also nicht AddNewEntity sondern "Added a new entity"
* Bevorzuge ImmutableArray anstatt Arrays oder Lists
* Wenn du GitHub Issues anlegst, füge sie immer dem Projekt "v0.1" (Nummer: `5`) hinzu, es sei denn es wird anders gesagt
* GitHub Issues immer auf Englisch schreiben, mit `## Summary` und ggf. `## Motivation`, `## Requirements`, `## Implementation notes` etc.
* Kein Co-Authored-By oder sonstiges AI-Branding in Commits
* Wenn ein Service mit zugehörigem Interface erstellt wird (z.B. IDockerService + DockerService), beides in die gleiche Datei schreiben, benannt nach der Implementierung (z.B. DockerService.cs, ohne "I"-Prefix)
* Keine statischen Methoden auf Services aufrufen — immer über DI injizieren und Instanzmethoden verwenden
