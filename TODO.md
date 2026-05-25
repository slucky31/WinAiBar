 Recommandation : référence le fichier, ne copie-colle pas. Trois raisons :
  1. Les prompts contiennent des blocs ``` imbriqués → le copier-coller casse souvent le formatage.
  2. Une source unique évite que tu modifies un prompt dans la conversation sans le répercuter dans le fichier.
  3. Tu peux laisser Claude relire le fichier à chaque étape pour qu'il ne dérive pas.

  Workflow recommandé (Claude Code)

  À chaque étape, ouvre une session fraîche (ou /clear) dans le dossier C:\Users\Nicolas\source\repos\WinAIBar et envoie :

  Lis le fichier PROMPTS.md à la racine du projet et exécute UNIQUEMENT le Prompt 15.
  - Ne regarde pas les prompts suivants.
  - Respecte strictement la convention de réponse listée en annexe du document.
  - Stoppe dès que les acceptance criteria sont remplis.
  - Si tu dois t'écarter du prompt (erreur build, package introuvable…), demande validation avant.
  - A la fin de chaque étape, le projet doit compiler, tous les tests doivent passés
  - Je m'occupe de la gestion de git

  Pour les prompts suivants, change juste le numéro :
  Lis PROMPTS.md et exécute UNIQUEMENT le Prompt 02. Pré-requis Prompt 01 déjà commit.

  Entre chaque prompt

  dotnet build           # doit être vert
  dotnet test            # doit être vert
  git add -A
  git commit -m "prompt-NN: <résumé court>"

  Puis /clear la session avant le prompt suivant — important pour ne pas accumuler les anciens diffs dans le contexte (sinon Claude ralentit et peut confondre fichiers passés/présents).

  Initialisation git (à faire une fois, avant le Prompt 01)

  cd C:\Users\Nicolas\source\repos\WinAIBar
  git init
  git add PROMPTS.md
  git commit -m "Initial: development plan (23 prompts)"

  Comme ça chaque prompt est commité par-dessus, et tu peux git reset --hard HEAD~1 si une étape rate.

  Si tu préfères l'interface web claude.ai

  Attache le PROMPTS.md en pièce jointe au début de la conversation, puis demande "Exécute le Prompt N". Mais tu n'as pas l'exécution de commandes — il te faudra recopier les diffs à la
  main dans VS. Claude Code est nettement plus efficace pour ce projet.


  dotnet run --project src\WinAIBar -p:Platform=x64