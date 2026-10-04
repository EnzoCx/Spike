# 0.4.5 — dépôt public et reprise du développement

- Séparation des modèles, calculs et validation JSON ; vérifications graphiques
  isolées des fichiers d’interaction WPF.
- Formatage harmonisé et imports inutilisés retirés de notre code.
- Suppression du paramètre inutilisé de Snapshot et de l’utilitaire amont inutilisé
  d’enregistrement brut de paquets.
- Bornes explicites pour le nombre d’intervalles d’une courbe ; tests de régression.
- Mode de vérification indépendant des préférences personnelles du poste.
- SDK et dépendances verrouillés ; script de validation partagé avec la CI Windows.
- Guide de reprise, architecture, contribution, signalement de sécurité,
  modèles d’issue/PR et notices tierces.
- Notes publiques débarrassées des pseudonymes de parties réelles ; planche de marque
  régénérée sans capture personnelle.

Validation locale : compilation sans avertissement, 53 contrôles Core, régressions
protocole, 23 contrôles d’opacité, 16 contrôles de placement et rendus FR/EN/ES.
Formatage vérifié ; aucune vulnérabilité NuGet connue signalée lors de la vérification
du 4 octobre 2026. Il ne s’agit pas d’un audit exhaustif de sécurité ou du protocole.

Les limites d’attribution et de roster de 0.4.4 restent explicites dans HANDOFF.md.
