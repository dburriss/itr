
- not sure local .itr/itr.json is discovered as expected. Need to check. For instance with `itr profile set-default --local`.
- it was mentioned that config changing usecases do not do the actual saving but return the updated config. This is not how usecases are supposed to work.
- `--profile` flag is not needed on every command, so shoould not be on `itr`
- `--output` flag is not needed on every command, so should not be on `itr` 
- `profile list` uses | instead  of \t for delimiter