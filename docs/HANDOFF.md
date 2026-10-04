# Reprendre le projet

État de référence : version 0.4.14, 4 octobre 2026.
Voir `CONTRIBUTING.md` pour les prérequis et les vérifications de développement.

## Ce qui fonctionne

Application Windows WPF, capture passive avec Npcap déjà installé, exécutable autonome
.NET, calculs dégâts/soins, filtre boss/toutes cibles, compétences, historique local,
import/export JSON v2. Interface FR/EN/ES, trois thèmes et identité graphique commune.
Bouton Copier dans le rapport et l’overlay : une ligne compacte pour le chat du jeu,
dans la langue de l’interface, avec noms, cible, durée, DPS/HPS global et classement
selon le filtre affiché, y compris dans l’historique. Valeurs abrégées en k/M,
sans rangs ni pourcentages dans le texte copié ; tous les noms sont conservés.
Les sources non identifiées et les données de démonstration restent signalées.
Présentation discrète activée par défaut : une ligne par joueur, valeurs abrégées,
barres de 3 px, commandes discrètes et hauteur ajustée au classement (défilement
au-delà de huit lignes). Le fond Graphite est atténué, puis renforcé au survol ;
les textes restent opaques pendant le combat. Ivoire et Contraste élevé conservent
leur lisibilité. Les détails restent au survol et au clic. Les options permettent
de retrouver la présentation détaillée ; les anciens réglages sont conservés.
Overlay séparé au premier plan : survol détaillé, compétences au clic, accès aux combats
archivés, taille automatique, mode compact, verrouillage des clics, aimantation aux bords,
placement par coin et recentrage. Pendant la lecture d’une archive, un bouton accentué
permet le retour à l’actuel en un clic ; la flèche voisine ouvre le sélecteur d’archives.
Hors combat : 15 % d’opacité après 12 secondes sans
événement, réapparition au combat/survol, fondu de 300 ms. Lecture d’archive toujours lisible.
Après deux minutes hors combat, l’overlay se réduit à sa barre de titre (44 px de haut).
Le survol ne le déplie pas ; le bouton flèche donne deux nouvelles minutes de lecture.
Le prochain combat restaure la taille précédente. Les archives restent dépliées, les
menus et déplacements en cours retardent la réduction. La taille réduite n’est pas
enregistrée comme préférence ; la transparence hors combat reste indépendante.

## Clôture des combats en monde ouvert

Si le personnage local est identifié, `LiveMeter` clôture après 12 secondes sans
dégâts infligés/reçus ni soin direct vers autrui. Les invocations ne comptent pour
ce délai que si leur propriétaire est connu. Le farm alentour ne relance pas le délai
et ne démarre pas de nouveau segment à lui seul. Les soins reçus, personnels et
périodiques ne maintiennent pas le combat actif.
Les événements observés pendant le segment restent conservés, sources anonymes comprises :
ce changement ne filtre pas le classement en groupe confirmé.
Sans identité locale, le mode d’observation conserve le délai global précédent.
Une identification tardive recalcule le délai depuis les événements conservés.

Pour un boss identifié et engagé, les dégâts de tous les participants sur cette
même entité maintiennent désormais le parse. Une phase silencieuse met le meter au
repos, mais sa reprise complète la même archive. `BossAttempt` utilise les resets
horodatés de `Aion2HitPoints` (retour au maximum observé après une baisse sous 95 %)
et les PV à zéro pour sceller une tentative. Un autre boss engagé, même du même nom,
démarre un nouveau parse ; le changement de zone/personnage, la pause et Terminer
effacent aussi la continuation. Terminer reste disponible pendant le repos d’un boss.
Les soins directs lient le soigneur au combat observé de leur destinataire, sans
déduire un roster. Les adds et soins personnels pendant les phases restent dans le parse.

## Sources supplémentaires

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
- Sans boss identifié, une phase sans activité personnelle de 12 secondes peut découper
  un combat. Pour un boss, un reset/mort non reçu peut fusionner des tentatives sur la
  même entité ; une mécanique de retour complet des PV peut être prise pour un reset.
  La reprise ne traverse pas le redémarrage du meter. Voir `docs/ARCHITECTURE.md`.
- Les joueurs observés ne sont pas un groupe confirmé ; noms et PV peuvent manquer.
- Npcap n’est pas embarqué ; aucune installation automatique ni approbation NCSOFT.
  L’assistant au lancement guide son installation s’il manque, puis revérifie sa
  présence. « Plus tard » laisse les archives accessibles ; « Démarrer » permet de réessayer.
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
