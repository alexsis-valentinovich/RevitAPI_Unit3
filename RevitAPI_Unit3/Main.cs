using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPI_Unit3
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                GroupPickFilter groupPickFilter = new GroupPickFilter();
                Reference reference = uiDoc.Selection.PickObject(ObjectType.Element, groupPickFilter, "Выберите группу объектов");
                Element element = doc.GetElement(reference);

                Group group = element as Group;
                XYZ groupCenter = GetElementCenter(group);

                Room room = GetRoomByPoint(doc, groupCenter);// комната, куда вставлена группа
                XYZ roomCenter = GetElementCenter(room);

                XYZ point = uiDoc.Selection.PickPoint("Выберите точку");

                Room roomPaste = GetRoomByPoint(doc, point);// комната, куда вставляем группу
                XYZ roomPasteCenter = GetElementCenter(roomPaste);

                XYZ offset = groupCenter - roomCenter;
                XYZ pointPaste = roomPasteCenter + offset;

                TaskDialog.Show("Center group", "X=" + Convert.ToString(groupCenter.X) + " Y=" + Convert.ToString(groupCenter.Y) + " Z=" + Convert.ToString(groupCenter.Z)
                + "\n" + "X=" + Convert.ToString(roomCenter.X) + " Y=" + Convert.ToString(roomCenter.Y) + " Z=" + Convert.ToString(roomCenter.Z));
                Transaction ts = new Transaction(doc);
                ts.Start("Копирование группы объектов");
                doc.Create.PlaceGroup(pointPaste, group.GroupType);
                ts.Commit();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        public XYZ GetElementCenter(Element element)
        {
            BoundingBoxXYZ bounding = element.get_BoundingBox(null);
            return (bounding.Max + bounding.Min) / 2;
        }

        public Room GetRoomByPoint(Document doc, XYZ point)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_Rooms);
            foreach (Element element in collector)
            {
                Room room = element as Room;
                if (room != null)
                {
                    if (room.IsPointInRoom(point))
                    {
                        return room;
                    }
                }
            }
            return null;
        }
    }

    public class GroupPickFilter : ISelectionFilter
    {
        bool ISelectionFilter.AllowElement(Element elem)
        {
            if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_IOSModelGroups)
                return true;
            else
                return false;
        }
        bool ISelectionFilter.AllowReference(Reference reference, XYZ position)
        {
            return false;
        }
    }
}
