# itr

An agentic workflow tool

## Usage (CLI)

### Global flags

- `--profile` / `-p` — select a portfolio profile
- `--output json` — machine-readable output

### profiles

```bash
# Add a profile
itr profiles add work

# Add with git identity and set as default
itr profiles add work --git-name "Jane Smith" --git-email "jane@example.com" --set-default
```

### product

```bash
# Scaffold a new product (prompts for id and repo-id if omitted)
itr product init ./my-project

# Register an existing product root in a profile
itr product register ./my-project
```

### backlog

```bash
# Create a new backlog item (repo auto-resolved for single-repo products)
itr backlog add my-feature --title "Add login page"

# Explicit repo — required for multi-repo products
itr backlog add my-feature --title "Add login page" --repo main-repo

# With optional fields
itr backlog add my-bug --title "Fix crash" --repo api --item-type bug --priority high --depends-on auth-feature

# Take a backlog item (creates task file(s))
itr backlog take my-feature
```
