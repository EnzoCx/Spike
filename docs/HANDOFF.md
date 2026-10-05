# Reprendre le projet

État de référence : version 0.5.12, 5 octobre 2026.
Voir `CONTRIBUTING.md` pour les prérequis et les vérifications de développement.

Depuis 0.5.12, les aperçus publics du rapport et de l'overlay utilisent une fixture
dédiée : cinq personnages fictifs, six compétences d'assassin avec icônes du catalogue.
`PublicPreviewFixture.cs` et les vérifications `*.PublicPreview.Verification.cs`
produisent les images sans capture ni fenêtre visible. Voir `site/README.md`.

Depuis 0.5.9, les mises à jour sont recherchées au lancement puis toutes les 15 minutes.
Une mise à jour prête affiche « Redémarrer pour mettre à jour » : fermeture normale avec
sauvegarde du combat, installation puis relance. Sans clic, elle attend la prochaine ouverture.
La vérification reste silencieuse et conserve le bouton prêt en cas de panne réseau.

Depuis 0.5.8, le site et le README comparent les fonctionnalités de Spike, NotMeter,
A2Tools et Abyss Logs, avec sources et date de consultation. Le site traduit ce
tableau en FR/EN/ES, dans les trois thèmes, avec défilement mobile et clavier et
contenu français disponible sans JavaScript. Voir `site/README.md` pour sa maintenance.

Depuis 0.5.5, l’aimantation de l’overlay agit à 6 pixels logiques des bords au lieu
de 12, avec adaptation à l’échelle de l’écran. Maj permet toujours de la contourner.

Maintenance 0.5.4 : SharpPcap 6.3.1 et PacketDotNet 1.4.8, avec les fichiers de
verrouillage régénérés pour toute la solution après le renommage en Spike.

## Identité et site

Le produit s’appelle **Spike** depuis 0.5.0, avec un symbole S en trois barres.
Le dépôt s’appelle `EnzoCx/Spike`. Depuis 0.5.2, l’exécutable `Spike.exe`, les projets,
les espaces de noms et les nouveaux chemins locaux portent tous le nom Spike. Les réglages
et combats DPSMeter sont copiés une fois sans supprimer les originaux ; en cas d’échec,
l’ancien dossier reste utilisé et la migration est retentée au lancement suivant.
Landing page FR/EN/ES et trois thèmes : https://enzocx.github.io/Spike/.
Sources dans `site/`, publication limitée au dossier assemblé par `tools/Build-Site.ps1`.
Les aperçus proviennent uniquement des fixtures démo hors écran. Voir `site/README.md`.
La migration vers 0.5.2 nécessite un téléchargement manuel de Spike.exe pour les anciennes installations.
La flèche de téléchargement près de la version relance la recherche et affiche son état.
Elle disparaît lorsque « Spike est à jour » est affiché et laisse place au bouton de
redémarrage lorsque la mise à jour est prête. Aucun redémarrage n’est imposé.

## Interface depuis 0.5.3

Direction inspirée de Codex : surfaces neutres, navigation avec icônes, rayons doux,
espacements harmonisés et police Geist embarquée. Les styles partagés sont dans
`CommonStyles.xaml` et les couleurs dans `Themes.cs`. Le bronze reste réservé au symbole.
Rapport, historique, réglages, installation Npcap, overlay, survols et menus utilisent
ces codes, en FR/EN/ES et dans les trois thèmes. Vérifications invisibles : police
réellement résolue (sans substitution), flèche de mise à jour, menus et tailles minimales.

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
Depuis 0.5.7, les nouvelles captures conservent les preuves explicites d'attribution,
invalident les associations périmées et préservent la source d'origine. Voir la section
Progression et fiabilité ci-dessous. Un roster complet n'est toujours pas garanti.

## Progression et fiabilité depuis 0.5.7

La page Progression compare les essais d'un même boss (identifiant catalogue), ou d'une
zone connue sans boss. Elle sélectionne le personnage local s'il est identifié, propose
l'essai précédent et le meilleur DPS précédent, montre les écarts par compétence et
les 20 derniers essais dans un graphique cliquable. Toutes les archives compatibles
restent sélectionnables. Le chargement est asynchrone ; la capture ne change pas la sélection.
Les anciennes archives sans identifiant de boss/serveur restent dans des séries séparées.
Démos, imports, zones et versions de protocole ne sont pas mélangés.

Morts observées : transitions PV positifs vers zéro, dédupliquées, dans les détails du
joueur, le survol overlay et les comparaisons. « Non enregistré » si aucune donnée ;
jamais de décompte garanti complet. Aucun écran de dégâts reçus.

Les heuristiques de propriétaire par classe/proximité de lancement et de nom par classe
ont été retirées. Une preuve d'ID ou de nom unique est nécessaire. Les événements gardent
leur source originale et une attribution figée à réception, sans réattribution des anciens
coups après réutilisation d'ID. Respawns, noms contradictoires et nouveaux contextes
invalident les associations périmées ; les noms de roster expirent après 90 secondes.
Une classe sans identité explicite reste une source non identifiée dans les nouvelles
captures, sans perte de ses dégâts. Les anciennes archives ne sont pas réécrites.
Démarrer en cours de session peut laisser le personnage local inconnu jusqu'à une annonce
explicite ; un changement d'instance silencieux reste indétectable.

Voir `EncounterProgress.cs`, `Dashboard.Progress.cs`, `Dashboard.Progress.Verification.cs`,
`Aion2EntityDirectory.Evidence.cs` et les nouveaux contrôles ProgressChecks/EvidenceChecks.

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
  installation sur clic ou au lancement suivant, ancienne version conservée. Voir `docs/UPDATES.md`.
  Téléchargement manuel de Spike.exe requis pour les versions antérieures à 0.5.2.
- SDK/runtime .NET 9 : migration LTS à planifier séparément, pas intégrée à ce nettoyage.

## Fichiers d’entrée

rDPS bêta depuis 0.5.11 : `docs/RDPS.md` décrit le modèle partiel de deux auras
(clerc/aède), l’hypothèse nominale 10,5 % niveau 1 et les essais à faire en jeu.
Rapport, overlay, survols, copie et archives v2 portent les limites ; le DPS brut
reste le défaut. `BetaBuffTracker.cs` suit les messages candidats, `RaidDamage.cs`
réattribue les bonus sans changer les totaux. Aucun cycle de vie ni coefficient
Global n’est validé. Aucune capture lancée pour ce développement. Sources et
protocole de calibration : `docs/RDPS-RESEARCH.md`, outils hors ligne : `tools/rdps/`.

`docs/ARCHITECTURE.md`, `docs/FORMAT.md`, `docs/DECISIONS.md`, `CONTRIBUTING.md`,
`THIRD-PARTY-NOTICES.md`. Présentation : `Dashboard.xaml`, `Dashboard.xaml.cs`,
`OverlayWindow.cs`, `CombatantRow.cs`, `CombatHoverCard.cs`, `Text.cs`.
Capture : `LiveMeter.cs`, `Vendor/Aion2/Protocol/Aion2FrameDecoder.cs`,
`Vendor/Aion2/Aion2EntityDirectory.cs`.

## Validation et données

Le fonctionnement des mises à jour et la procédure de publication sont décrits dans `docs/UPDATES.md`.

`tools/Verify.ps1` lance les tests hors ligne et les rendus hors écran. Aucun clic ni fenêtre
visible au-dessus du jeu. La CI Windows utilise le même script.
Les données réelles restent dans `%LOCALAPPDATA%/Spike`, jamais dans Git.
`artifacts/` est local et ignoré. Ne pas publier ses captures de combats réels.
Les tests sont des programmes console : leur code de sortie est l’autorité, pas `dotnet test`.
Les comptes rendus de validation passée ne sont pas une certification du protocole.

Les vérifications ordinaires doivent rester hors ligne et ne pas interrompre les applications ouvertes.
