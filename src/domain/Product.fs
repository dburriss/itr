namespace Itr.Domain

type ProductId = private ProductId of string

/// Coordination configuration as read from product.yaml
type CoordinationConfig =
    { Mode: string
      Repo: string option
      Path: string option }

and RepoConfig = { Path: string; Url: string option }

/// Canonical product definition loaded from product.yaml
type ProductDefinition =
    { Id: ProductId
      Description: string option
      Repos: Map<string, RepoConfig>
      Docs: Map<string, string>
      Coordination: CoordinationConfig
      CoordRoot: CoordinationRoot }

type ProductConfig =
    { Id: ProductId
      Repos: Map<RepoId, RepoConfig> }

type ResolvedProduct =
    { Profile: Profile
      Product: ProductRef
      Definition: ProductDefinition
      CoordRoot: CoordinationRoot }

[<RequireQualifiedAccess>]
module ProductId =
    let private rules = "must match [a-z0-9][a-z0-9-]*"

    let tryCreate (value: string) : Result<ProductId, PortfolioError> =
        if Validation.isValidSlug value then
            Ok(ProductId value)
        else
            Error(InvalidProductId(value, rules))

    let value (ProductId value) = value

/// Capability interface for loading product configuration from product.yaml
type IProductConfig =
    /// Load product config from <productRoot>/product.yaml
    abstract LoadProductConfig: productRoot: string -> Result<ProductDefinition, PortfolioError>
