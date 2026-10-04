# DPSMeter 0.4.1 — les repères visuels d’AION 2

Les anciens symboles génériques sont remplacés par les emblèmes de classes
d’AION 2 disponibles sur le site NotMeter. Les barres utilisent ses mêmes couleurs
de classes : assassin vert, rôdeur turquoise, gladiateur bleu, aède doré, etc.
La marque et les thèmes de DPSMeter sont conservés pour la navigation.

Les compétences ont maintenant leur véritable icône dans le survol du joueur,
le détail de l’overlay et le rapport complet. L’association utilise l’identifiant
du jeu : elle est identique en français, anglais et espagnol. Les variantes d’une
même compétence partagent l’image correspondante. Les identifiants inconnus restent
neutres, sans image inventée ni rapprochement approximatif sur le nom.

## Ressources récupérées en ligne et embarquées

- 9 emblèmes de classes.
- Atlas Global AION 2 et correspondances pour 15 671 identifiants de compétences
  et 3 693 identifiants de buffs, incluant des variantes partageant une image.
- Couleurs de classes issues du site NotMeter ; ce sont ses conventions visuelles,
  pas une spécification de couleurs publiée par NCSOFT.
- Aucune image à télécharger au lancement ou pendant un combat.

**Buffs :** les images et leur correspondance sont disponibles dans le catalogue.
La capture de leurs durées et leur affichage dans les rapports restent à implémenter.
Cette version n’ajoute pas de statistiques de buffs artificielles.

## Sources

- [NotMeter](https://notmeter.com/) : emblèmes et couleurs de classes.
- [Catalogue Global](https://notmeter.com/assets/global-details/icons.json?v=20261004-ui-bindings)
  et [atlas Global](https://notmeter.com/assets/global-details/icons.webp?v=20261004-ui-bindings).
- [Palette de classes dans le site](https://notmeter.com/assets/app.js?v=20261004-ranker-region).

Instantané du 4 octobre 2026. Les URL et empreintes SHA-256 sont conservées dans
le projet. La conversion WebP vers PNG conserve exactement les pixels décodés.
Les images du jeu restent la propriété de NCSOFT ; la licence MIT couvre notre code.
Les crédits sont aussi consultables dans les réglages du logiciel.

## Vérifications et lancement

Compilation sans avertissement, 39 contrôles de calcul/stockage et contrôles
d’interface hors écran dans les trois langues/thèmes. Vérification des neuf classes,
du chargement des tuiles, des variantes de compétences, du cas inconnu et de la
séparation des identifiants de buffs. Inspection visuelle sur le combat d’Atiel enregistré.

Fermer uniquement l’ancienne fenêtre DPSMeter, puis lancer **DPSMeter-0.4.1.exe**.
Le jeu peut rester ouvert. Les historiques et réglages existants sont repris.
