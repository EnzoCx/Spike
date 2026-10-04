# Contribuer

Lire `docs/HANDOFF.md` et `docs/ARCHITECTURE.md` avant toute modification.
Prérequis : Windows x64, SDK .NET 9.0.304 (global.json) et Git. Npcap et le jeu ne sont pas nécessaires
pour compiler ou exécuter les contrôles hors ligne.

```powershell
git clone https://github.com/EnzoCx/Spike.git
cd Spike
powershell -NoProfile -File tools/Verify.ps1
```

Le résultat autonome se trouve dans `artifacts/windows/DPSMeter.exe`.
Les captures synthétiques et résultats de contrôle sont dans `artifacts/verification`.
Pour lancer réellement la capture, Npcap doit déjà être installé. Ne jamais installer
un pilote ou redémarrer le jeu pour une vérification de code.

## Changements

- Une correction ciblée, des noms explicites et peu d’abstractions.
- Lire les fichiers concernés avant de les modifier.
- Conserver le JSON v2 existant, les langues FR/EN/ES et les trois thèmes.
- Tester les régressions fonctionnelles significatives, notamment les calculs.
- Préserver les notices amont. Éviter les reformatages de `Vendor/`.
- `dotnet format DPSMeter.sln --exclude src/DPSMeter.Engine/Vendor` harmonise notre code.
- Si une dépendance change, régénérer et relire les `packages.lock.json`.

Une PR décrit le problème, le comportement obtenu, les vérifications et les limites.
Aucun paquet réseau, combat réel non anonymisé, secret ou donnée de compte dans les
issues, PR, captures ou fixtures. Utiliser des exemples synthétiques clairement marqués.
Voir `SECURITY.md` pour signaler un problème sensible.
