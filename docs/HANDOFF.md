# Reprendre le projet

État de référence : version 0.4.6, 4 octobre 2026.
Voir `CONTRIBUTING.md` pour les prérequis et les vérifications de développement.

## Ce qui fonctionne

Application Windows WPF, capture passive avec Npcap déjà installé, exécutable autonome
.NET, calculs dégâts/soins, filtre boss/toutes cibles, compétences, historique local,
import/export JSON v2. Interface FR/EN/ES, trois thèmes et identité graphique commune.
Overlay séparé au premier plan : survol détaillé, compétences au clic, accès aux combats
archivés, taille automatique, mode compact, verrouillage des clics, aimantation aux bords,
placement par coin et recentrage. Hors combat : 15 % d’opacité après 12 secondes sans
événement, réapparition au combat/survol, fondu de 300 ms. Lecture d’archive toujours lisible.

## Dernier problème traité : « joueurs » supplémentaires

Certaines sources anonymes n’utilisent que Mur de feu, Tempête glaciale ou Piège explosif.
Le préfixe de classe d’une compétence ne prouve pas qu’il s’agit d’un joueur.
`EncounterSources.Classify` les marque provisoirement `isUnidentifiedSource`.
Les sources apparues comme PNJ et utilisant des compétences de classe sont également
séparées dans `LiveMeter`. Une ligne dépliable de l’overlay conserve leurs détails.
Leurs dégâts restent dans le total et ne sont PAS affectés à un joueur supposé.
Un aède sans nom utilisant plusieurs compétences reste dans les participants.

Cette correction est une classification prudente, pas une résolution des propriétaires
ni un roster fiable. Ne pas masquer ce problème derrière un filtre des cinq premiers DPS.
Les anciennes sauvegardes n’ont pas les trames d’apparition/propriétaire nécessaires.
Prochaine amélioration utile : capturer les preuves d’appartenance et d’identité au bon
moment, gérer leur durée de validité et tester deux joueurs de même classe, les invocations,
les changements d’instance et les IDs réutilisés. Préserver les événements originaux.

## Autres limites connues

- Les critiques des autres joueurs peuvent être incomplets.
- Buff uptime, dos/face, doubles et coups parfaits ne sont pas exposés.
- Une phase sans événement de 12 secondes peut découper un combat.
- Les joueurs observés ne sont pas un groupe confirmé ; noms et PV peuvent manquer.
- Npcap n’est pas embarqué ; aucune installation automatique ni approbation NCSOFT.
- Pas de site communautaire, classements en ligne ou signature Windows.
- Mise à jour automatique depuis GitHub Releases : téléchargement en arrière-plan,
  installation au lancement suivant, ancienne version conservée. Voir `docs/UPDATES.md`.
  Première installation manuelle requise pour les utilisateurs de 0.4.5 ou antérieur.
- SDK/runtime .NET 9 : migration LTS à planifier séparément, pas intégrée à ce nettoyage.

## Fichiers d’entrée

`docs/ARCHITECTURE.md`, `docs/FORMAT.md`, `docs/DECISIONS.md`, `CONTRIBUTING.md`,
`THIRD-PARTY-NOTICES.md`. Présentation : `Dashboard.xaml`, `Dashboard.xaml.cs`,
`OverlayWindow.cs`, `CombatantRow.cs`, `CombatHoverCard.cs`, `Text.cs`.
Capture : `LiveMeter.cs`, `Vendor/Aion2/Protocol/Aion2FrameDecoder.cs`,
`Vendor/Aion2/Aion2EntityDirectory.cs`.

## Validation et données

Le fonctionnement des mises à jour et la procédure de publication sont décrits dans `docs/UPDATES.md`.

`tools/Verify.ps1` lance les tests hors ligne et les rendus hors écran. Aucun clic ni fenêtre
visible au-dessus du jeu. La CI Windows utilise le même script.
Les données réelles restent dans `%LOCALAPPDATA%/DPSMeter`, jamais dans Git.
`artifacts/` est local et ignoré. Ne pas publier ses captures de combats réels.
Les tests sont des programmes console : leur code de sortie est l’autorité, pas `dotnet test`.
Les comptes rendus de validation passée ne sont pas une certification du protocole.

Les vérifications ordinaires doivent rester hors ligne et ne pas interrompre les applications ouvertes.
