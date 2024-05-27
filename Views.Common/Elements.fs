namespace Views.Common

open System.Windows
open Telerik.Windows.Controls

type PaneId = System.Guid
type CustomDocumentPane() as this =
    inherit RadDocumentPane()

    static let mutable _dockedIdProperty: DependencyProperty = null
    static do _dockedIdProperty <- DependencyProperty.Register("DockedId", typeof<System.Guid>, typeof<CustomDocumentPane>, new PropertyMetadata())

    do
        this.SetBinding(CustomDocumentPane.IsHiddenProperty, "IsHidden") |> ignore

    
    static member DockedIdProperty with get () = _dockedIdProperty

    member x.DockedId with get() : System.Guid = x.GetValue(CustomDocumentPane.DockedIdProperty) :?> System.Guid and set (v:System.Guid) = x.SetValue(CustomDocumentPane.DockedIdProperty, v)
    



    
