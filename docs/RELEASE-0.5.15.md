# Spike 0.5.15

- Retrait du mode rDPS expérimental : rapport, mini-meter, survols, copie et suivi des auras. Les anciennes archives restent lisibles et leurs dégâts bruts sont conservés.
- Retrait de sa présentation sur le site, le README et la documentation ; aperçus publics actualisés sans le bouton.
- Correction d'une remise à zéro observée après la mort de Bakarma : les derniers dégâts reçus sur le boss complètent désormais la même archive terminée, sans ouvrir un nouveau combat presque vide. Les réapparitions et nouvelles tentatives restent séparées.
- Vérification de la reprise du même combat après une phase silencieuse de cinq minutes. Le temps sans dégâts entre deux phases reste inclus dans la fenêtre du boss ; l'attente après le dernier coup ne dilue pas le DPS.

Validation : contrôles hors ligne du moteur, compatibilité des anciennes archives, suite complète `tools/Verify.ps1` et rendus FR/EN/ES dans les trois thèmes. Aucune capture ni interaction avec le jeu. La coupure spécifiquement signalée pendant une phase de Bakarma n'a pas été reproduite en jeu ; cette correction traite le défaut confirmé des dégâts après sa mort.
