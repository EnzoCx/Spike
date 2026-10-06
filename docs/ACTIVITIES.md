# Activités : planning et checklist

Ajout préparé le 6 octobre 2026. Les horaires sont des préréglages **Global**,
sans téléchargement à l’exécution. Ils ne certifient ni la présence d’un boss ni
un calendrier officiel. Une nouvelle release Windows doit accompagner la mise en
ligne des textes du site annonçant ces fonctions.

## Utilisation

- **Activités → Checklist** : choisir un personnage, puis Quotidien ou Hebdomadaire.
  Cocher termine l’objectif ; décocher remet son compteur à zéro. Les boutons −/+
  permettent une progression partielle. « Personnaliser » masque des tâches,
  ajuste les objectifs et ajoute des tâches ou des personnages. Les définitions
  des tâches sont communes ; chaque personnage a ses propres compteurs.
- **Overlay → ✓** : la même checklist, immédiatement synchronisée. **DPS** revient
  au combat, qui continue d’être capturé pendant la consultation. La checklist
  reste lisible et dépliée. Clic droit sur ✓ ouvre l’accès à la page Activités.
  Déverrouiller d’abord l’overlay s’il laisse passer les clics.
- **Activités → Événements & rappels** : activer les notifications, puis cocher les
  événements souhaités. Le délai (0 à 60 minutes), le son, le décalage UTC, l’heure
  quotidienne et le jour hebdomadaire sont configurables. « Tester une notification »
  utilise les derniers réglages enregistrés, même si l’interrupteur principal est coupé.
- Chaque événement peut recevoir des horaires `HH:mm` séparés par des virgules et
  des jours de semaine. Une modification devient un horaire personnalisé. Les
  événements personnalisés peuvent être supprimés. Les sources s’ouvrent uniquement
  sur clic. Aucune complétion ni interaction avec le jeu n’est automatisée.

Les notifications sont désactivées au premier lancement. Shugo et Rift sont
présélectionnés ; les autres rappels demandent un choix explicite. Une petite fenêtre
non modale apparaît pendant 12 secondes, sans activation ni vol de focus. Les
événements simultanés sont groupés. Elle peut être fermée ou ouvrir Activités.
Les rappels fonctionnent capture arrêtée, overlay masqué et fenêtre principale
minimisée. **Fermer la fenêtre principale arrête les rappels**, y compris en mode
« Lancer avec le jeu », lorsque Spike retourne à l’attente. Le plein écran exclusif
peut cacher les fenêtres de bureau ; utiliser le mode fenêtré/sans bordures.

## Sources et choix des préréglages

Consultation le **6 octobre 2026** :

| Source | Apport et limites |
| --- | --- |
| [GuideMMO — checklist](https://guidemmo.com/checklist-aion-2/) (mise à jour 5 octobre) | Inspiration des activités quotidiennes/hebdomadaires et des objectifs initiaux. Le reset annoncé à 05:00 ne précise pas de fuseau ; ce chiffre n’est donc pas appliqué tel quel à Global. |
| [AION2 Hub — Event Timer](https://aion2hub.com/tools/event-timer) | Rift Global toutes les 3 heures, à partir de 00:00, et reset Global quotidien à 16:00 UTC+9, soit 07:00 UTC ; mercredi pour la semaine. |
| [Shugo.GG — Timers](https://shugo.gg/timers) (calendrier daté 1er octobre) | Shugo à chaque heure ; planning communautaire Global présenté en UTC, sièges et boss. L’invasion est explicitement non confirmée sur Global. |
| [Aion2 Guide — horaires](https://aion2.run/en/horaires) (mise à jour 5 octobre) | Corroboration du reset Global 07:00 UTC ; réserves sur les boss Kaira/Nahma et sur l’invasion. |

Préréglages stockés en **UTC** : Shugo chaque heure, Rift 00:00 puis toutes les
3 heures, sièges lundi/jeudi/samedi à 21:00 et boss de siège à 21:30. Les propositions
Nahma vendredi/dimanche 21:00, Kaira 01:00 puis toutes les 3 heures et invasion à
chaque demi-heure portent **« À confirmer en jeu »**, avec rappels désactivés.
Les occurrences indiquent le début prévu, pas une fenêtre d’accès garantie.
Gartua n’a pas de point de départ fixe suffisamment établi dans les sources
consultées : aucun horaire absolu n’est inventé ; l’utilisateur peut ajouter son
propre événement. Aucun préréglage de calendrier spécifique Corée/Taïwan n’est annoncé.

Les sources **divergent** sur l’horloge Global : Shugo.GG affiche un reset à 16:00 UTC,
contre 07:00 UTC chez AION2 Hub et Aion2 Guide. Spike retient 07:00 UTC pour le reset,
tout en exposant clairement son réglage. Le cycle Rift de trois heures est invariant
au décalage de neuf heures ; cette équivalence ne vaut pas pour les boss hebdomadaires.
Leur statut communautaire et leur source restent visibles. NCSOFT n’a pas été trouvé
comme source directe d’un calendrier Global complet lors de cette recherche.

Les heures affichées dans le planning et les rappels suivent le fuseau Windows,
avec ses changements d’heure. La saisie et les resets utilisent un **décalage UTC
fixe**, affiché explicitement. Modifier ce décalage déplace tous les horaires saisis ;
modifier le reset recalcule la période des cases déjà cochées. Le reset initial
07:00 UTC correspond à 09:00 à Paris en été et 08:00 en hiver.

## Stockage et responsabilités

`Spike.Core/Activities.cs` contient les modèles, le catalogue, la validation,
les calculs temporels purs, la déduplication et le stockage atomique.
`ActivityController` possède l’état partagé et le seul ordonnanceur. Il est appelé
par le timer de Dashboard, sans dépendance au moteur de capture. `ActivitiesView`
sert la page complète et l’overlay ; ses libellés viennent de `Text.Activities.cs`.
`ActivityToast` présente les alertes ; aucun service cloud, compte ni API externe.

`%LOCALAPPDATA%\Spike\activities.json` est distinct des préférences et des combats.
Chaque compteur conserve son horodatage de modification. Le début de période est
calculé à la lecture : les jours/semaines d’absence n’exigent aucune exécution en
arrière-plan. Un fichier illisible est **conservé**, les écritures étant bloquées avec
une explication dans l’interface ; aucune restauration silencieuse ne l’écrase.
Les sauvegardes réussissent avant publication du nouvel état aux deux vues.

Les identifiants d’occurrence déjà notifiés sont sauvegardés avant l’affichage et
conservés deux jours. Une reprise après veille ne rejoue pas un backlog : seule la
dernière minute de retard est tolérée. Il n’y a ni rappel différé ni synthèse des
événements manqués. Les limites du fichier sont 2 Mio, 100 événements, 100 tâches,
20 personnages et 24 horaires par événement. Les éléments personnalisés sont locaux.

## Vérification

```sh
dotnet run --project tests/Spike.Checks -c Release -- --activities-only
```

Ces 49 contrôles sont portables : frontières exactes, changements d’heure et de
jour, semaines d’absence, personnages séparés, modifications d’objectif, notifications
simultanées, veille, désactivation, redémarrage, fichier invalide et sauvegarde atomique.

Sous Windows, `tools/Verify.ps1` inclut aussi `VerifyActivities` : navigation,
coches, synchronisation overlay, notifications sans affichage réel et rendus hors
écran FR/EN/ES × trois thèmes, aux tailles habituelles et minimales. Les résultats
sont écrits dans `artifacts/verification/activities-result.txt` et les images
`activities-*`, `schedule-*`, `checklist-overlay-*`. Aucun jeu ni Npcap n’est requis.
Le toast réel et son son demandent encore une vérification Windows avec le bouton
« Tester une notification ». L’environnement Linux ne peut pas exécuter WPF.
