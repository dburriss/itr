
- not sure local .itr/itr.json is discovered as expected. Need to check. For instance with `itr profile set-default --local`.
- it was mentioned that config changing usecases do not do the actual saving but return the updated config. This is not how usecases are supposed to work.
- `--profile` flag is not needed on every command, so shoould not be on `itr`
- `--output` flag is not needed on every command, so should not be on `itr` 
- `profile list` uses | instead  of \t for delimiter
- `product info` and `product list` search for product by walking up the dirctory tree. This will not work since the product.yaml is in a subdirectory. The correct way would be to look up products in active profile and the check coordination directory for each one against the current directory.
- I am not sure about the backlog item fetching and whether it needs to have seperate methods for fetching active vs. archived backlog items. I would expect the same method to be able to fetch both, and the caller can filter as needed. Probably want to look at filtering and async...
- `task-list` has id and backlog columns which are duplicates. One should be removed.