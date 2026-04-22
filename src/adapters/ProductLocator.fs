namespace Itr.Adapters

open System.IO
open Itr.Domain

[<RequireQualifiedAccess>]
module ProductLocator =

    /// Walk up the directory tree from `startDir` looking for `product.yaml`.
    /// Returns `Some rootDir` if found, `None` if not found before the filesystem root.
    let locateProductRoot (fs: IFileSystem) (startDir: string) : string option =
        let rec tryFind (dir: string) =
            let candidate = Path.Combine(dir, "product.yaml")
            if fs.FileExists candidate then
                Some dir
            else
                let parent = Path.GetDirectoryName(dir)
                if isNull parent || parent = dir then
                    None
                else
                    tryFind parent
        tryFind startDir
