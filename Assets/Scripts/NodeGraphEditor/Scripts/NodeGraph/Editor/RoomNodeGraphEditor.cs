using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace VelascoGames.NodeGraphEditor
{
	public class RoomNodeGraphEditor : EditorWindow
	{
		private GUIStyle roomNodeStyle;
		private GUIStyle roomNodeSelectedSytle;

		private Vector2 graphOffset;
		private Vector2 graphDrag;

		private static RoomNodeGraphSO currentRoomNodeGraph;
		private RoomNodeSO currentRoomNode = null;
		private RoomNodeTypeListSO roomNodeTypeList;

		//Node layout values
		private const float nodeWidth = 160f;
		private const float nodeHeight = 75f;
		private const int nodePadding = 25;
		private const int nodeBorder = 12;

		//Connecting line values
		private const float connectingLineWidth = 3f;
		private const float connectingLineArrowSize = 6f;

		// Grid Spacing
		private const float gridLarge = 100f;
		private const float gridSmall = 25f;


		[MenuItem("Room Node Graph Editor", menuItem = "Window/Dungeon Editor/Room Node Graph Editor")]
		private static void OpenWindow()
		{
			GetWindow<RoomNodeGraphEditor>("Room Node Graph Editor");
		}

		private void OnEnable()
		{
			//Subscribe to the inspector selection changed event
			Selection.selectionChanged += InspectoSelectionChanged;

			//Define node layout style
			roomNodeStyle = new GUIStyle();
			roomNodeStyle.normal.background = EditorGUIUtility.Load("node1") as Texture2D;
			roomNodeStyle.normal.textColor = Color.white;
			roomNodeStyle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
			roomNodeStyle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

			//Define selected node style
			roomNodeSelectedSytle = new GUIStyle();
			roomNodeSelectedSytle.normal.background = EditorGUIUtility.Load("node1 on") as Texture2D;
			roomNodeSelectedSytle.normal.textColor = Color.white;
			roomNodeSelectedSytle.padding = new RectOffset(nodePadding, nodePadding, nodePadding, nodePadding);
			roomNodeSelectedSytle.border = new RectOffset(nodeBorder, nodeBorder, nodeBorder, nodeBorder);

			// Load Room node types
			roomNodeTypeList = GameResources.Instance.roomNodeTypeList;
		}

		private void OnDisable()
		{
			//Unsubscribe to the inspector selection changed event
			Selection.selectionChanged -= InspectoSelectionChanged;
		}

		/// <summary>
		/// Open the room node graph editor window if a room node graph scriptable object asset is double clicked in the inspector
		/// </summary>
		[OnOpenAsset(0)]
		public static bool OnDoubleClickAsset(int instanceID, int line)
		{
			RoomNodeGraphSO roomNodeGraph = EditorUtility.InstanceIDToObject(instanceID) as RoomNodeGraphSO;

			if (roomNodeGraph != null)
			{
				OpenWindow();

				currentRoomNodeGraph = roomNodeGraph;

				return true;
			}
			return false;
		}



		private void OnGUI()
		{
			//If a scriptable object of type RoomNodeGraphSO has been selected the process
			if (currentRoomNodeGraph != null)
			{
				//Draw grid
				DrawBackgroundGrid(gridSmall, 0.2f, Color.gray);
				DrawBackgroundGrid(gridLarge, 0.3f, Color.gray);

				//Draw line if being dragged
				DrawDraggedLine();

				//Process Events
				ProcessEvents(Event.current);

				//Draw Connection Between Room Nodes
				DrawRoomConnections();

				//Draw Room Nodes
				DrawRoomNodes();
			}

			if (GUI.changed)
				Repaint();

			//Lo siguiente va a moverse a otra clase
			/*
			GUILayout.BeginArea(new Rect(new Vector2(100f, 100f), new Vector2(nodeWidth, nodeHeight)), roomNodeStyle);
			EditorGUILayout.LabelField("Node 1");
			GUILayout.EndArea();

			GUILayout.BeginArea(new Rect(new Vector2(300f, 300f), new Vector2(nodeWidth, nodeHeight)), roomNodeStyle);
			EditorGUILayout.LabelField("Node 1");
			GUILayout.EndArea(); */
		}

		/// <summary>
		/// Draw a background grid for the room node graph editor
		/// </summary>
		private void DrawBackgroundGrid(float gridSize, float gridOpacity, Color gridColor)
		{
			int verticalLineCount = Mathf.CeilToInt((position.width + gridSize) / gridSize);
			int horizontalLineCount = Mathf.CeilToInt((position.height + gridSize) / gridSize);

			Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);

			graphOffset += graphDrag * 0.5f;

			Vector3 gridOffset = new Vector3(graphOffset.x % gridSize, graphOffset.y % gridSize, 0);

			for (int i = 0; i < verticalLineCount; i++)
			{
				Handles.DrawLine(new Vector3(gridSize * i, -gridSize, 0) + gridOffset, new Vector3(gridSize * i, position.height + gridSize, 0f) + gridOffset);
			}

			for (int j = 0; j < horizontalLineCount; j++)
			{
				Handles.DrawLine(new Vector3(-gridSize, gridSize * j, 0) + gridOffset, new Vector3(position.width + gridSize, gridSize * j, 0f) + gridOffset);
			}

			Handles.color = Color.white;
		}

		private void DrawDraggedLine()
		{
			if (currentRoomNodeGraph.linePosition != Vector2.zero)
			{
				//Draw line from node to line position
				Handles.DrawBezier(currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition,
					currentRoomNodeGraph.roomNodeToDrawLineFrom.rect.center, currentRoomNodeGraph.linePosition, Color.white, null, connectingLineWidth);
			}
		}

		private void ProcessEvents(Event currentEvent)
		{
			//Reset graph drag
			graphDrag = Vector2.zero;

			//Get room node that mouse is over if it's null or not currently being dragged
			if (currentRoomNode == null || currentRoomNode.isLeftClickDragging == false)
			{
				currentRoomNode = IsMouseOverRoomNode(currentEvent);
			}

			//if mouse isn't over a room node or we are currently dragging a line from the room node to then process graph events
			if (currentRoomNode == null || currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
			{
				ProcessRoomNodeGraphEvents(currentEvent);
			}
			//else process room node events
			else
			{
				//process room node events
				currentRoomNode.ProcessEvents(currentEvent);
			}
		}

		private RoomNodeSO IsMouseOverRoomNode(Event currentEvent)
		{
			for (int i = currentRoomNodeGraph.roomNodeList.Count - 1; i >= 0; i--)
			{
				if (currentRoomNodeGraph.roomNodeList[i].rect.Contains(currentEvent.mousePosition))
				{
					return currentRoomNodeGraph.roomNodeList[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Process Room Node Graph Events
		/// </summary>
		private void ProcessRoomNodeGraphEvents(Event currentEvent)
		{
			switch (currentEvent.type)
			{
				// Process Mouse Down Events
				case EventType.MouseDown:
					ProcessMouseDownEvent(currentEvent);
					break;

				// Process Mouse Up Events
				case EventType.MouseUp:
					ProcessMouseUpEvent(currentEvent);
					break;

				// Process Mouse Drag Events
				case EventType.MouseDrag:
					ProcessMouseDragEvent(currentEvent);
					break;

				default:
					break;
			}
		}

		/// <summary>
		/// Process mouse down events on the room node graph (not over a node)
		/// </summary>
		private void ProcessMouseDownEvent(Event currentEvent)
		{
			//Process right click mouse down on graph event (show context menu)
			if (currentEvent.button == 1)
			{
				ShowContextMenu(currentEvent.mousePosition);
			}
			//Process left mouse down on graph event
			else if (currentEvent.button == 0)
			{
				ClearLineDrag();
				ClearAllSelectedRoomNodes();
			}
		}

		/// <summary>
		/// Show the context menu
		/// </summary>
		private void ShowContextMenu(Vector2 mousePosition)
		{
			GenericMenu menu = new GenericMenu();

			menu.AddItem(new GUIContent("Create Room Node"), false, CreateRoomNode, mousePosition);

			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Select All Room Nodes"), false, SelectAllRoomNodes);
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Delete Selected Room Nodes"), false, DeleteSelectedRoomNodes);
			menu.AddItem(new GUIContent("Delete Selected Room Nodes Link"), false, DeleteSelectedRoomNodesLinks);

			menu.ShowAsContext();
		}

		/// <summary>
		/// Delete selected room nodes
		/// </summary>
		private void DeleteSelectedRoomNodes()
		{
			Queue<RoomNodeSO> roomNodeDeletionQueue = new Queue<RoomNodeSO>();

			foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
			{
				if (roomNode.isSelected && !roomNode.roomNodeType.isEntrance)
				{
					roomNodeDeletionQueue.Enqueue(roomNode);

					//Iterate through child room node ids
					foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
					{
						//Retrieve child room node
						RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(childRoomNodeID);

						if (childRoomNode != null)
						{
							//Remove parentID from child room node
							childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
						}
					}

					//Iterate through parent room node ids
					foreach (string parentRoomNodeID in roomNode.parentRoomNodeIDList)
					{
						//Retrieve parent node
						RoomNodeSO parentRoomNode = currentRoomNodeGraph.GetRoomNode(parentRoomNodeID);

						if (parentRoomNode != null)
						{
							//Remove childID from parent node
							parentRoomNode.RemoveChildRoomNodeIDFromRoomNode(roomNode.id);
						}
					}

				}
			}

			//Delete queued room nodes
			while (roomNodeDeletionQueue.Count > 0)
			{
				RoomNodeSO roomToDelete = roomNodeDeletionQueue.Dequeue();

				//remove node from dictionary
				currentRoomNodeGraph.roomNodeDictionary.Remove(roomToDelete.id);

				//remove node from list
				currentRoomNodeGraph.roomNodeList.Remove(roomToDelete);

				//remove from asset database
				DestroyImmediate(roomToDelete, true);

				AssetDatabase.SaveAssets();
			}
		}

		private void DeleteSelectedRoomNodesLinks()
		{
			foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
			{
				if (roomNode.isSelected && roomNode.childRoomNodeIDList.Count > 0)
				{
					for (int i = roomNode.childRoomNodeIDList.Count - 1; i >= 0; i--)
					{
						//Get child room node
						RoomNodeSO childRoomNode = currentRoomNodeGraph.GetRoomNode(roomNode.childRoomNodeIDList[i]);

						//If the child room node is selected
						if (childRoomNode != null && childRoomNode.isSelected)
						{
							//Remove childID from parent room node
							roomNode.RemoveChildRoomNodeIDFromRoomNode(childRoomNode.id);

							//Remove parentID from child room node
							childRoomNode.RemoveParentRoomNodeIDFromRoomNode(roomNode.id);
						}
					}
				}
			}
			//Clear all selected room nodes
			ClearAllSelectedRoomNodes();

		}


		/// <summary>
		/// Select all room nodes
		/// </summary>
		private void SelectAllRoomNodes()
		{
			foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
			{
				roomNode.isSelected = true;
			}
			GUI.changed = true;
		}

		/// <summary>
		/// Create a room node at the mouse position
		/// </summary>
		private void CreateRoomNode(object mousePositionObject)
		{
			// If current node graph emtpy then add entrance room node first
			if (currentRoomNodeGraph.roomNodeList.Count == 0)
			{
				CreateRoomNode(new Vector2(200f, 200f), roomNodeTypeList.list.Find(x => x.isEntrance));
			}

			CreateRoomNode(mousePositionObject, roomNodeTypeList.list.Find(x => x.isNone));
		}

		/// <summary>
		/// Clear Selection from all room nodes
		/// </summary>
		private void ClearAllSelectedRoomNodes()
		{
			foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
			{
				if (roomNode.isSelected)
				{
					roomNode.isSelected = false;

					GUI.changed = true;
				}
			}
		}

		/// <summary>
		/// Create a room node at the mouse position - overloaded to also pass in RoomNodeType
		/// </summary>
		private void CreateRoomNode(object mousePositionObject, RoomNodeTypeSO roomNodeType)
		{
			Vector2 mousePosition = (Vector2)mousePositionObject;

			//create room node scriptable object asset
			RoomNodeSO roomNode = ScriptableObject.CreateInstance<RoomNodeSO>();

			// add room node scriptable object asset
			currentRoomNodeGraph.roomNodeList.Add(roomNode);

			//setroom node values
			roomNode.Initialise(new Rect(mousePosition, new Vector2(nodeWidth, nodeHeight)), currentRoomNodeGraph, roomNodeType);

			//add room node to room node graph scriptable object asset database
			AssetDatabase.AddObjectToAsset(roomNode, currentRoomNodeGraph);

			AssetDatabase.SaveAssets();

			//Refresh graph node dictionary
			currentRoomNodeGraph.OnValidate();
		}

		/// <summary>
		/// Process mouse up mouse events
		/// </summary>
		/// <param name="currentEvent"></param>
		private void ProcessMouseUpEvent(Event currentEvent)
		{
			if (currentEvent.button == 1 && currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
			{
				//Check if over a room node
				RoomNodeSO roomNode = IsMouseOverRoomNode(currentEvent);

				if (roomNode != null)
				{
					//if so set it as a child of the parent room node if it can be added
					if (currentRoomNodeGraph.roomNodeToDrawLineFrom.AddChildRoomNodeIDToRoomNode(roomNode.id))
					{
						//Set parent ID in child room node
						roomNode.AddParentRoomNodeIDToRoomNode(currentRoomNodeGraph.roomNodeToDrawLineFrom.id);
					}
				}

				ClearLineDrag();
			}

		}

		/// <summary>
		/// Process mouse drag event
		/// </summary>
		private void ProcessMouseDragEvent(Event currentEvent)
		{
			//process right click drag event - draw line
			if (currentEvent.button == 1)
			{
				ProcessRightMouseDragEvent(currentEvent);
			}

			if (currentEvent.button == 0)
			{
				ProcessLeftMouseDragEvent(currentEvent.delta);
			}
		}

		/// <summary>
		/// Drag connecting line from room node
		/// </summary>
		public void DragConnectingLine(Vector2 delta)
		{
			currentRoomNodeGraph.linePosition += delta;
		}

		/// <summary>
		/// Process right mouse drag event - draw line
		/// </summary>
		private void ProcessRightMouseDragEvent(Event currentEvent)
		{
			if (currentRoomNodeGraph.roomNodeToDrawLineFrom != null)
			{
				DragConnectingLine(currentEvent.delta);
				GUI.changed = true;
			}
		}

		/// <summary>
		/// Process left mouse drag event - drag room node graph
		/// </summary>
		private void ProcessLeftMouseDragEvent(Vector2 dragDelta)
		{
			graphDrag = dragDelta;

			for (int i = 0; i < currentRoomNodeGraph.roomNodeList.Count; i++)
			{
				currentRoomNodeGraph.roomNodeList[i].DragNode(dragDelta);
			}

			GUI.changed = true;
		}

		/// <summary>
		/// Clear line drag from a room node
		/// </summary>
		private void ClearLineDrag()
		{
			currentRoomNodeGraph.roomNodeToDrawLineFrom = null;
			currentRoomNodeGraph.linePosition = Vector2.zero;
			GUI.changed = true;
		}

		/// <summary>
		/// Draw connections in the graph window between room nodes
		/// </summary>
		private void DrawRoomConnections()
		{
			foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
			{
				if (roomNode.childRoomNodeIDList.Count > 0)
				{
					//Loop throughchild room nodes
					foreach (string childRoomNodeID in roomNode.childRoomNodeIDList)
					{
						DrawConnectionLine(roomNode, currentRoomNodeGraph.roomNodeDictionary[childRoomNodeID]);

						GUI.changed = true;
					}
				}
			}
		}

		/// <summary>
		/// Draw connection line between the parent room node and child room node
		/// </summary>
		private void DrawConnectionLine(RoomNodeSO parentRoomNode, RoomNodeSO childRoomNode)
		{
			//getline start and end position
			Vector2 startPosition = parentRoomNode.rect.center;
			Vector2 endPosition = childRoomNode.rect.center;

			//calculate midway point
			Vector2 midPosition = (endPosition + startPosition) / 2f;

			//Vector from start to end position of line
			Vector2 direction = endPosition - startPosition;

			//Calculate normalised perpendicular position from the mid point
			Vector2 arrowTailPoint1 = midPosition - new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;
			Vector2 arrowTailPoint2 = midPosition + new Vector2(-direction.y, direction.x).normalized * connectingLineArrowSize;

			//Calculate mid point offset position for arrow head
			Vector2 arrowHeadPoint = midPosition + direction.normalized * connectingLineArrowSize;

			//Draw arrow
			Handles.DrawBezier(arrowHeadPoint, arrowTailPoint1, arrowHeadPoint, arrowTailPoint1, Color.white, null, connectingLineWidth);
			Handles.DrawBezier(arrowHeadPoint, arrowTailPoint2, arrowHeadPoint, arrowTailPoint2, Color.white, null, connectingLineWidth);

			//Draw line
			Handles.DrawBezier(startPosition, endPosition, startPosition, endPosition, Color.white, null, connectingLineWidth);

			GUI.changed = true;
		}

		/// <summary>
		/// Draw room nodes in the graph window
		/// </summary>
		private void DrawRoomNodes()
		{
			// Loop through all room nodes and draw them
			foreach (RoomNodeSO roomNode in currentRoomNodeGraph.roomNodeList)
			{
				if (roomNode.isSelected)
				{
					roomNode.Draw(roomNodeSelectedSytle);
				}
				else
				{
					roomNode.Draw(roomNodeStyle);
				}

			}

			GUI.changed = true;
		}

		/// <summary>
		/// Selection changed in the inspector
		/// </summary>
		private void InspectoSelectionChanged()
		{
			RoomNodeGraphSO roomNodeGraph = Selection.activeObject as RoomNodeGraphSO;

			if (roomNodeGraph != null)
			{
				currentRoomNodeGraph = roomNodeGraph;
				GUI.changed = true;
			}
		}



	}

}
