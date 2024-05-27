module MainProgram

open Elmish
open Elmish.WPF
open SciChart.Charting.Model.DataSeries.Heatmap2DArrayDataSeries
open Views
open System
open Telerik.Windows.Controls.Docking;
open Telerik.Windows.Controls;
open Views.Common

type Model = {
    DockedModels: ChartPage.Model array
}

type Msg =
    | AddDockedModel
    | ChartPageMsg of Guid * ChartPage.Msg
    | PanesClosed of Views.Common.PaneId list
    | Error of exn

let garbageCollect () =
    GC.Collect()

let setupModelCmd () =
    let id = Guid.NewGuid()
    let m, cmd = ChartPage.init (id)
    m,
    Cmd.map (fun c -> ChartPageMsg(id, c)) cmd

let convertDockCloseArgs (args:obj) = 
    match args with
    | :? Telerik.Windows.Controls.Docking.StateChangeEventArgs as a ->
        a.Panes 
        |> Seq.choose (function | :? CustomDocumentPane as p -> Some p.DockedId | _ -> None)
    | _ -> Seq.empty
    |> List.ofSeq

let init(): Model*Cmd<Msg> = 
    {
        DockedModels = [||]
    }, Cmd.none

let update (msg: Msg) (model: Model) : Model*Cmd<Msg> =
    match msg with
    | Error e -> printfn "!! %O" e; model, Cmd.none
    | AddDockedModel -> 
        let m, cmd = setupModelCmd()
        { model with DockedModels = model.DockedModels |> Array.append [|m|] }, cmd
    | ChartPageMsg (id, msg) ->
        match msg with
        | ChartPage.Msg.RemoveDockedModel -> { model with DockedModels = model.DockedModels |> Array.filter (fun x -> x.Id <> id)}, Cmd.OfFunc.attempt garbageCollect () Error
        | _ ->
            let updatedModel, paneCmd =
                        model.DockedModels
                        |> Array.tryFind (fun m -> m.Id = id)
                        |> Option.map (fun subModel -> 
                            let subModel', subCmd = ChartPage.update msg subModel
                            { model with 
                                DockedModels = 
                                    model.DockedModels 
                                    |> Array.map (fun m -> if m.Id = id then subModel' else m) },
                            Cmd.map (fun c -> ChartPageMsg(id, c)) subCmd)
                        |> Option.defaultValue (model, Cmd.none)
            updatedModel, Cmd.batch [ paneCmd;]

    | PanesClosed closeIds ->
        model, closeIds |> List.map (fun id -> ChartPageMsg (id, ChartPage.Close) |> Cmd.ofMsg) |> Cmd.batch
        

let bindings (): Binding<Model, Msg> list = [
    "DockCloseCmd" |> Binding.cmdParam (fun p m -> PanesClosed (convertDockCloseArgs p))
    "DockedModels" |> Binding.subModelSeq ((fun (m: Model) -> m.DockedModels), snd, (fun (m: ChartPage.Model) -> m.Id), (ChartPageMsg), ChartPage.bindings)
    "AddDockedModel" |> Binding.cmd AddDockedModel
    "RegenDockedModels" |> Binding.cmd RegenDockedModels
]

[<EntryPoint; STAThread>]
let main _ =
  Program.mkProgramWpf init update bindings
  |> Program.runWindow (MainWindow())
