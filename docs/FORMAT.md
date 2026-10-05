# Formats des combats

## JSON v2 — application actuelle

Contrat `Encounter` dans `src/Spike.Core/Encounter.cs`. Version 2, UUID de combat,
date avec fuseau, région Global, version du protocole, origine, zone, motif de fin,
durée en millisecondes. Participants : ID, nom, classe, joueur/boss. Événements :
temps relatif, source, cible, ID/nom de compétence, quantité, soin, critique et tick.
Maximum 64 Mo, 250 000 événements, 4 096 entités et 24 heures.
La capture coupe aussi les segments au seuil de 250 000 événements.

Depuis 0.4, les participants acceptent des champs facultatifs compatibles avec les
anciens fichiers v2 : `combatPower`, `isSelf`, `currentHp`, `highestHp`.
Depuis 0.4.4, `isUnidentifiedSource` (false par défaut) distingue une source non
confirmée comme joueur. Elle conserve ses dégâts dans les totaux sans augmenter
le nombre de joueurs. Cette classification n’identifie pas le propriétaire.
L’absence d’information donne `null` (ou `false` pour `isSelf`). `highestHp` est
un maximum observé, pas une garantie des PV maximum du jeu. L’export enlève `isSelf`.
Les événements restent inchangés : le filtre boss et le regroupement des variantes
de compétences sont appliqués à la lecture, sans altérer les sauvegardes.

La sauvegarde conserve les noms. L'export remplace les noms des joueurs et tous les
IDs d'entités. Les métadonnées libres ne constituent pas une garantie d'anonymat.
Les imports sont non vérifiés ; une démo conserve son marqueur de démo.

## Métadonnées facultatives depuis 0.5.7

Participants : `npcId` (identifiant catalogue du boss), `serverId` (serveur du personnage
local lorsqu'il est reçu), `observedDeaths` (nombre de transitions PV positifs → zéro
observées), `identityEvidence` (`direct` ou `unknown`). Les anciens fichiers restent
valides : absence de compteur = information non enregistrée, jamais zéro décès prouvé.

Événements : `originalSource` conserve l'entité d'invocation avant attribution ;
`attribution` précise `owner-id` ou `owner-name`. La source créditée est figée à la
réception ; une réutilisation d'ID ne réattribue pas les anciens dégâts. L'export remplace
également cet identifiant original et conserve les références valides. Ces métadonnées
ne prouvent pas l'authenticité d'un import.

## Métadonnées facultatives rDPS bêta

`rdpsModel: "auras-level1-v1"` identifie le modèle provisoire. Un événement de dégâts
peut porter `raid: { "provider": 2, "skillId": 18190000, "bonus": 105 }`.
`amount` conserve les dégâts bruts ; `bonus` est une contribution figée, comprise
entre zéro et ces dégâts. Le fournisseur doit exister dans les participants ;
seules les familles 17410000 et 18190000 sont acceptées. Une aura personnelle a
un bonus transféré nul. Aucun crédit n’est accepté sur un soin ou sans modèle.
L’export remplace également `provider`. L’absence de métadonnées signifie inconnu,
jamais zéro buff. Voir [RDPS.md](RDPS.md) pour les hypothèses et limites.

## JSON v1 — ancien prototype, non importé par l'interface actuelle

UTF-8, 10 Mo maximum. Voir `samples/combat.spike.json`.

| Champ | Rôle |
| --- | --- |
| schemaVersion | Exactement 1 |
| id | Identifiant local du combat |
| region | Global |
| patch | Version du jeu ou synthetic-v1 pour la démonstration |
| boss, difficulty | Contexte du combat ; libellés de prototype |
| durationMs | Durée explicite, > 0, <= 24 h |
| isDemo | Données synthétiques ; conservé à l'export |
| actors | 1 à 100 participants : id, name, className |
| hits | Jusqu'à 100 000 événements : id, offsetMs, actorId, skill, damage, critical |

Les IDs de participants et d'événements doivent être uniques. Les événements hors
durée, dégâts négatifs et participants inconnus sont refusés. Les textes sont
limités à 120 caractères. L'ordre des événements n'affecte pas les totaux.

DPS = somme des dégâts / (durationMs / 1000), y compris le temps sans attaque.
Part = dégâts du participant / dégâts du groupe ; pour une compétence, dégâts de
la compétence / dégâts du participant. Critiques = nombre d'événements critiques /
nombre d'événements du groupe considéré. Un groupe vide donne 0, jamais NaN.

`isDemo=false` n'implique aucune authenticité : un import est toujours non vérifié.
Le format actuel n'est ni un parseur du protocole Aion 2 ni un import NotMeter.
L'export remplace les noms et IDs des acteurs, IDs des événements et ID du combat.
Il conserve les libellés libres de boss, classe, compétence et contexte : vérifier
ces champs avant de partager un fichier importé provenant d'une autre personne.
