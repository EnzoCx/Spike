# Architecture

```text
Npcap existant → TCP serveur → réassemblage → décodeur AION 2
              → LiveMeter → Encounter v2 → historique local
                                        → calculs → rapport / overlay
```

## Responsabilités

| Projet | Rôle | Dépendances |
| --- | --- | --- |
| Core | Modèles, validation, calculs, historique, classification prudente | Bibliothèque .NET |
| Engine | Capture passive et adaptation des événements | Core, SharpPcap, PacketDotNet |
| Desktop | Présentation WPF et orchestration locale | Core, Engine |
| Checks | Régressions des calculs et du stockage | Core |
| EngineChecks | Réassemblage/protocole hors réseau | Engine |

`Encounter.cs` définit les données ; `EncounterMath.cs` calcule les classements ;
`EncounterFile.cs` valide/import/exporte ; `EncounterStore.cs` sauvegarde atomiquement.
`EncounterSources.cs` distingue les sources anonymes suspectées d’être des effets.

La capture alimente une file bornée ; le timer de présentation appelle `LiveMeter.Poll`
chaque seconde. Depuis 0.5.17, seuls les boss reconnus par le catalogue peuvent démarrer
un segment, même sans identité locale. Les dégâts sans lien avec le boss engagé sont
exclus du rapport et de l'historique, y compris les adds et le farm personnel.
Les soins sont conservés pendant le combat de boss, mais seuls les dégâts impliquant
ce boss prolongent son activité ou reprennent une phase au repos.

`BossEventBuffer` attend une identification tardive pendant au plus 30 secondes et
4 096 dégâts non confirmés, uniquement en mémoire. La source et son attribution sont
figées dès réception. Les dégâts confirmés sont restitués une seule fois, par date,
sans reprendre ceux d'une entité réapparue depuis. Les frontières de contexte, zone,
personnage, pause et fin manuelle effacent le tampon. Un boss absent du catalogue ou
dont l'annonce n'est pas reçue ne crée pas de combat ; le tampon ne constitue pas une archive.

`BossAttempt` suit le boss engagé par son identifiant d’entité et son identifiant PNJ,
jamais par son nom seul. L’engagement provient d’un échange de dégâts personnel ou du
combat observé d’un joueur soigné directement dans les 12 secondes précédentes.
Tous les dégâts impliquant ce boss maintiennent ensuite le combat, même si le joueur
local est mort ou inactif. Après 12 secondes de silence, le segment est sauvegardé et
mis au repos : `HasCombat` est faux et `Snapshot` est nul. Une reprise des dégâts
impliquant ce boss complète le même identifiant d’archive.
Le farm des autres cibles ne réactive pas cette continuation.

La mort observée (PV à zéro), un reset de PV, un autre boss engagé, un changement de
zone/personnage, une fin manuelle, une pause ou une limite de taille/durée scellent
le segment. Le reset utilise `Aion2HitPoints.ResetsOf` : retour au maximum observé
après une lecture inférieure à 95 % de ce maximum. Ses horodatages séparent les
tentatives même si plusieurs pulls arrivent dans le même lot. Les PV de mort au même
instant qu’un dégât sont appliqués après ce dégât pour conserver le coup final.
Le bouton Terminer reste disponible pour un boss au repos. La continuation reste en
mémoire pendant la session ; aucune fusion d'anciennes archives n'est effectuée.

Depuis 0.5.15, la mort clôture immédiatement l'affichage actif, mais conserve la
tentative en mémoire pour rattacher les derniers dégâts de cette même entité à
l'archive terminée. Les PV à zéro peuvent précéder ces événements. Ces ajouts ne
rouvrent pas le combat et ne remplacent pas le résultat par un segment de quelques
coups. Les PV redevenus positifs, une réapparition explicite, un autre boss
engagé ou les frontières de contexte existantes libèrent cette continuation.
Le maximum de taille/durée reste applicable. Sans preuve de réapparition reçue,
des dégâts sur la même entité restent associés à la tentative terminée.

Sans signal de reset/mort reçu, une nouvelle tentative sur la même entité peut être
confondue avec une phase ; une remontée complète des PV pendant une mécanique peut
inversement être prise pour un reset. Un changement d’instance non signalé avec les
mêmes identifiants ne peut pas être distingué. Le maximum observé n’est qu’une borne
inférieure si la capture commence au milieu du combat. Une fin manuelle reste le recours.
Le dernier combat reste affiché au repos. Un point de reprise est enregistré toutes
les 10 secondes ; reprendre un boss met à jour le même fichier v2, sans doublon.

Les sauvegardes sont sérialisées par un sémaphore. Le dernier enregistrement attendu
à la fermeture inclut les événements drainés par l’arrêt de capture.
Un combat sélectionné dans l’historique est indépendant du dernier combat reçu.

## Progression et preuves

`EncounterProgress` produit une projection légère de chaque archive : DPS dans la
fenêtre du boss, HPS sur tout le combat, durée, morts observées et compétences.
`EncounterStore.Progress` lit les fichiers en arrière-plan sans conserver leurs événements.
La page Progression reste stable pendant la capture et se rafraîchit explicitement.
Les 20 derniers essais apparaissent dans le graphique ; tous restent sélectionnables.

Les séries séparent région, protocole, origine, zone et identifiant catalogue du boss
(ou nom observé pour les anciennes archives). Sans boss, seule une zone connue permet
le regroupement. L'identité comparée utilise nom, classe et serveur lorsqu'il est connu,
jamais l'ID d'entité d'une session. Les homonymes ambigus dans un même combat sont exclus
de la progression. Les archives sans serveur ou identifiant boss sont séparées des
nouvelles séries qui disposent de ces preuves ; leurs résultats restent consultables.
La difficulté et la composition exacte du groupe ne sont pas garanties par la capture.

La classe et la proximité d'un lancement ne prouvent plus une identité ou un propriétaire.
Le décodeur conserve la source originale avec les preuves explicites d'attribution.
Les associations expirent au respawn ou au renouvellement du contexte ; les données
anonymes restent comptées séparément. Les informations reçues après un changement de
contexte ne renomment pas le dernier rapport déjà construit. Un changement silencieux
sans trame identifiable ne peut toujours pas être détecté. Au démarrage en cours de
session, l'identité locale peut rester inconnue jusqu'à sa prochaine annonce explicite.

Les morts utilisent les passages de PV positifs à zéro dans la fenêtre observée, y compris
après le dernier dégât et avant la clôture, sans étendre la durée servant au DPS. Des zéros
répétés ne comptent qu'une fois ; une remontée de PV permet un nouveau décès. Les phases
de boss restent dans le même rapport. Les relevés absents, les morts hors visibilité et
les premières lectures à zéro ne permettent pas un décompte exhaustif. Aucun écran de
dégâts reçus n'est ajouté.

## Overlay

Fenêtre WPF transparente autonome, sans injection dans le jeu. `OverlayPlacement`
limite ses propres coordonnées au moniteur et applique l’aimantation native.
L’opacité du fond et l’atténuation globale hors combat sont deux réglages distincts.
`CombatantRow` est actualisé en place. Les fiches utilisent le même modèle que le rapport.
Les tests graphiques résident dans les fichiers `*.Verification.cs` et rendent
hors écran avec `RenderTargetBitmap`, sans piloter le bureau.

## Ressources

Polices, marque, tables et icônes sont intégrées à la compilation. Les tables du moteur
sont extraites dans le cache local propre à Spike. Aucun téléchargement d’image à
l’exécution. `tools/Fetch-GameArtwork.py` est réservé à une mise à jour volontaire du
catalogue : URLs et empreintes sont conservées, les changements distants sont refusés.

## Compatibilité

`Desktop/Updates` gère les releases GitHub, le téléchargement vérifié, le cache par
chemin d'installation et le remplacement atomique au lancement suivant. `App` traite
le mode assistant avant toute création de fenêtre ou capture. Les tests hors ligne
de téléchargement utilisent un transport HTTP simulé et des fichiers temporaires.
Contrat et publication : `docs/UPDATES.md`.

Le contrat actif est JSON v2. Le v1 subsiste seulement dans les anciennes fixtures,
sans chemin d’import dans l’interface. Le site futur devra utiliser des contrats
validés côté serveur et ne jamais supposer qu’un import prouve un combat authentique.


## Activités

Le planning et la checklist sont indépendants des combats : modèles, horaires,
resets et sauvegarde dans `Core/Activities.cs`, état partagé dans
`Desktop/ActivityController.cs`, vues réutilisables `ActivitiesView` et notifications
`ActivityToast`. Le timer de Dashboard est le seul ordonnanceur. L’overlay accède au
même contrôleur et n’écrit pas de seconde copie. `activities.json` ne modifie ni
`settings.json` ni le format de combat v2. Sources et limites : [ACTIVITIES.md](ACTIVITIES.md).
