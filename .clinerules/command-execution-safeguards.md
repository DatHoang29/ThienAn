## Brief overview
This guideline defines best practices for running CLI commands, managing git workflows safely, and structuring commits to prevent shell syntax parsing errors and maintain repository health.

## Command execution
- Avoid using the `&&` operator or any character combinations containing `&` in tool parameters to prevent XML-to-shell parsing errors (such as `zsh: parse error near ';&'`).
- Run commands sequentially in separate tool calls, or use alternative command flags (e.g., `git -C <directory> <command>` instead of chaining `cd` and `&&`).
- Use non-interactive flags where possible, such as `git --no-pager`, to prevent the terminal from hanging in interactive pager screens.

## Commit management
- Keep unrelated features or infrastructure changes in separate, clean commits (e.g., keeping logger setup and Keycloak authentication integrations distinct).
- Use temporary Node.js scripts for bulk file copying or cleanup instead of long, nested bash commands to ensure cross-platform safety and avoid terminal parsing limits.