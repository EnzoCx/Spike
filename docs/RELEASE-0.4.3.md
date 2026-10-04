# DPSMeter 0.4.3 — discret hors combat

L’overlay entier, chiffres compris, descend à **15 % d’opacité hors combat**.
La fin du combat est détectée après 12 secondes sans événement, comme pour
l’enregistrement automatique. Le dernier résultat reste présent en transparence.

- Retour immédiatement lisible dès la réception d’un nouveau combat.
- Retour lisible au survol lorsque l’overlay est déverrouillé.
- Déplacement, redimensionnement et menus restent lisibles pendant l’interaction.
- Un combat choisi dans l’historique reste lisible pour permettre son analyse.
- Fondu de 300 ms à la sortie ; respecte la désactivation des animations Windows.
- **··· → Presque transparent hors combat** permet de désactiver cette option.
  Le choix est enregistré et traduit en français, anglais et espagnol.

Le réglage d’opacité du fond reste indépendant. En mode verrouillé, le survol ne
réveille pas l’overlay : les clics traversent toujours jusqu’au jeu. Les nouveaux
dégâts le rendent lisible automatiquement.

## Vérification

23 contrôles de visibilité hors écran : début/fin de combat, survol, menus,
déplacement, redimensionnement annulé, historique, verrouillage, pause, arrêt et
préférence enregistrable. Compatibilité des anciens réglages vérifiée.
Les 39 contrôles de calcul et d’historique, les 16 contrôles de placement et les
vérifications d’interface passent également. Aucun clic, déplacement du curseur,
arrêt de capture existante ou manipulation du jeu pendant ces essais.

Pour utiliser cette version, fermer uniquement l’ancien DPSMeter puis lancer
**DPSMeter-0.4.3.exe**. L’historique et les réglages sont conservés.
