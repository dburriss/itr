module Itr.Cli.AppDeps

open Itr.Domain
open Itr.Adapters

/// Composition root - combines all adapters into a single deps object
type AppDeps() =
    let envAdapter = EnvironmentAdapter()
    let fsAdapter = FileSystemAdapter()
    let portfolioConfigAdapter = PortfolioAdapter.PortfolioConfigAdapter(envAdapter, fsAdapter)
    let productConfigAdapter = YamlAdapter.ProductConfigAdapter()
    let backlogStoreAdapter = YamlAdapter.BacklogStoreAdapter()
    let taskStoreAdapter = YamlAdapter.TaskStoreAdapter()
    let viewStoreAdapter = YamlAdapter.ViewStoreAdapter()
    let agentHarnessAdapter = OpenCodeHarnessAdapter()

    interface IEnvironment with
        member _.GetEnvVar name =
            (envAdapter :> IEnvironment).GetEnvVar name

        member _.HomeDirectory() =
            (envAdapter :> IEnvironment).HomeDirectory()

    interface IFileSystem with
        member _.ReadFile path =
            (fsAdapter :> IFileSystem).ReadFile path

        member _.WriteFile path content =
            (fsAdapter :> IFileSystem).WriteFile path content

        member _.FileExists path =
            (fsAdapter :> IFileSystem).FileExists path

        member _.DirectoryExists path =
            (fsAdapter :> IFileSystem).DirectoryExists path

    interface IPortfolioConfig with
        member _.ConfigPath() =
            (portfolioConfigAdapter :> IPortfolioConfig).ConfigPath()

        member _.LoadConfig path =
            (portfolioConfigAdapter :> IPortfolioConfig).LoadConfig path

        member _.SaveConfig path portfolio =
            (portfolioConfigAdapter :> IPortfolioConfig).SaveConfig path portfolio

    interface IProductConfig with
        member _.LoadProductConfig productRoot =
            (productConfigAdapter :> IProductConfig).LoadProductConfig productRoot

    interface IBacklogStore with
        member _.LoadBacklogItem coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).LoadBacklogItem coordRoot backlogId

        member _.LoadArchivedBacklogItem coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).LoadArchivedBacklogItem coordRoot backlogId

        member _.ArchiveBacklogItem coordRoot backlogId date =
            (backlogStoreAdapter :> IBacklogStore).ArchiveBacklogItem coordRoot backlogId date

        member _.BacklogItemExists coordRoot backlogId =
            (backlogStoreAdapter :> IBacklogStore).BacklogItemExists coordRoot backlogId

        member _.WriteBacklogItem coordRoot item =
            (backlogStoreAdapter :> IBacklogStore).WriteBacklogItem coordRoot item

        member _.ListBacklogItems coordRoot =
            (backlogStoreAdapter :> IBacklogStore).ListBacklogItems coordRoot

        member _.ListArchivedBacklogItems coordRoot =
            (backlogStoreAdapter :> IBacklogStore).ListArchivedBacklogItems coordRoot

    interface IViewStore with
        member _.ListViews coordRoot =
            (viewStoreAdapter :> IViewStore).ListViews coordRoot

    interface ITaskStore with
        member _.ListTasks coordRoot backlogId =
            (taskStoreAdapter :> ITaskStore).ListTasks coordRoot backlogId

        member _.ListArchivedTasks coordRoot backlogId =
            (taskStoreAdapter :> ITaskStore).ListArchivedTasks coordRoot backlogId

        member _.WriteTask coordRoot task =
            (taskStoreAdapter :> ITaskStore).WriteTask coordRoot task

        member _.ArchiveTask coordRoot backlogId taskId date =
            (taskStoreAdapter :> ITaskStore).ArchiveTask coordRoot backlogId taskId date

        member _.ListAllTasks coordRoot =
            (taskStoreAdapter :> ITaskStore).ListAllTasks coordRoot

    interface IAgentHarness with
        member _.Prompt prompt debug =
            (agentHarnessAdapter :> IAgentHarness).Prompt prompt debug
