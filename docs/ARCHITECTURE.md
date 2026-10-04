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
chaque seconde. Un segment finit après 12 secondes sans événement, à la demande ou à
une limite de taille/durée. Le dernier combat reste affiché, mais ne signifie pas
qu’un combat est actif. Un point de reprise est enregistré toutes les 10 secondes.

Les sauvegardes sont sérialisées par un sémaphore. Le dernier enregistrement attendu
à la fermeture inclut les événements drainés par l’arrêt de capture.
Un combat sélectionné dans l’historique est indépendant du dernier combat reçu.

## Overlay

Fenêtre WPF transparente autonome, sans injection dans le jeu. `OverlayPlacement`
limite ses propres coordonnées au moniteur et applique l’aimantation native.
L’opacité du fond et l’atténuation globale hors combat sont deux réglages distincts.
`CombatantRow` est actualisé en place. Les fiches utilisent le même modèle que le rapport.
Les tests graphiques résident dans les fichiers `*.Verification.cs` et rendent
hors écran avec `RenderTargetBitmap`, sans piloter le bureau.

## Ressources

Polices, marque, tables et icônes sont intégrées à la compilation. Les tables du moteur
sont extraites dans le cache local propre à DPSMeter. Aucun téléchargement d’image à
l’exécution. `tools/Fetch-GameArtwork.py` est réservé à une mise à jour volontaire du
catalogue : URLs et empreintes sont conservées, les changements distants sont refusés.

## Compatibilité

Le contrat actif est JSON v2. Le v1 subsiste seulement dans les anciennes fixtures,
sans chemin d’import dans l’interface. Le site futur devra utiliser des contrats
validés côté serveur et ne jamais supposer qu’un import prouve un combat authentique.
