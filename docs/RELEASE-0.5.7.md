# Spike 0.5.7

- Nouvelle page **Progression**, accessible depuis un rapport ou l'historique : regroupement par boss ou zone connue et personnage, comparaison avec l'essai précédent ou le meilleur DPS précédent, évolution sur les 20 derniers essais et sélection de toutes les archives compatibles.
- Comparaison du DPS, de la durée, du HPS et des compétences. Les démos, imports, versions de protocole et identités incompatibles restent séparés ; les limites de comparaison sont indiquées.
- **Morts observées** dans les détails du joueur, le survol de l'overlay et les comparaisons. Les anciens combats sans relevés affichent « Non enregistré ». Aucun écran de dégâts reçus n'est ajouté.
- Attribution plus prudente : fin des déductions de nom ou de propriétaire basées sur la classe ou la proximité d'un lancement. Conservation de la source d'invocation originale et de la preuve d'attribution, invalidation des associations périmées et protection contre la réattribution lors d'une réutilisation d'ID.
- Format JSON v2 et anciennes préférences conservés. Interface en français, anglais et espagnol, dans les trois thèmes.

Les morts comptées sont les transitions de PV effectivement observées, pas un décompte garanti exhaustif. Les archives anciennes sans identifiant de boss ou de serveur restent regroupées séparément. La difficulté et la composition exacte du groupe ne sont pas garanties ; un écart de DPS ne prouve pas à lui seul une amélioration du build. L'identité locale peut rester inconnue lors d'un démarrage en cours de session.

Validation : contrôles hors ligne des calculs, du protocole, des attributions et des morts ; compatibilité et anonymisation JSON ; tests invisibles de navigation, comparaison, stabilité pendant les mises à jour et rendu dans les trois langues et thèmes, aux tailles normale et minimale. Aucun test de capture en jeu.
