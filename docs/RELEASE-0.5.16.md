# Spike 0.5.16 — activités, rappels et overlay compact

Spike démarre avec son overlay. La fenêtre principale reste accessible par double-clic sur l’icône près de l’horloge Windows ; fermer cette fenêtre conserve la capture, l’overlay et les rappels. **Quitter Spike** dans le menu de l’icône arrête complètement le logiciel.

- **Activités** : checklist quotidienne et hebdomadaire, réserves de clés et d’énergie, personnages distincts et limites partagées par serveur. Les achats d’Odyle au marché Brisevent sont séparés en 4 par personnage et 16 par serveur chaque semaine.
- **Réserves** : boutons − / + par 1 pour les clés et tentatives, par 40 pour l’Odyle. Saisie exacte du stock, plafonds personnalisables et aide sur l’activité, son accès et ses sources. Les stocks restent manuels et ne sont jamais réinitialisés comme des objectifs quotidiens.
- **Rappels configurables** : événements, horaires, jours, référence UTC et anticipation. Les notifications s’empilent en haut à droite, avec file d’attente, animation discrète et carillon original désactivable. Elles fonctionnent même lorsque la capture est arrêtée.
- **Un seul en-tête** : compteurs courts comme « Shugo [00:42] · Rift [02:42] », en heures:minutes, à côté du titre et des commandes. L’option « Afficher les compteurs d’événements » conserve la vue dégâts. Les noms complets et horaires locaux restent au survol ; les commandes s’adaptent aux petites largeurs.
- **Réglages plus compacts** : opacités par curseurs avec aperçu immédiat, onglets Compteur / Tâches dans l’en-tête, bouton d’ajout d’événement mieux intégré et suppression du cadre blanc lors du clic sur les menus de navigation.
- **Documentation et site** : nouveaux aperçus Windows, explications et sources Global recoupées. Les horaires communautaires restent modifiables ; les boss incertains sont désactivés par défaut.

Les rappels sont désactivés au départ : activez-les dans **Activités → Événements et rappels**. L’overlay et ses compteurs sont activés par défaut ; les choix déjà enregistrés sont conservés. Au besoin, ouvrez la fenêtre principale depuis l’icône système pour accéder à l’assistant Npcap.

Les sauvegardes de checklist précédentes sont migrées sans transformer des réalisations en clés restantes. Le fichier original est conservé au premier enregistrement dans `activities.json.v1.bak` ou `activities.json.v2.bak`, selon sa version.

Validation : suite Windows `tools/Verify.ps1`, contrôles de stockage, de migration et de rappels, rendus et interactions hors écran FR/EN/ES dans les trois thèmes, puis 28 scénarios du site. Aucun jeu ni capture réelle utilisés pour ces vérifications.
