/* Copyright (C) Itseez3D, Inc. - All Rights Reserved
* You may not use this file except in compliance with an authorized license
* Unauthorized copying of this file, via any medium is strictly prohibited
* Proprietary and confidential
* UNLESS REQUIRED BY APPLICABLE LAW OR AGREED BY ITSEEZ3D, INC. IN WRITING, SOFTWARE DISTRIBUTED UNDER THE LICENSE IS DISTRIBUTED ON AN "AS IS" BASIS, WITHOUT WARRANTIES OR
* CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED
* See the License for the specific language governing permissions and limitations under the License.
* Written by Itseez3D, Inc. <support@avatarsdk.com>, April 2017
*/

using UnityEngine;
using System.Collections.Generic;
using System;

namespace ItSeez3D.AvatarSdk.Core
{
	public static class MeshUtils
	{
		public class Edge
		{
			public int v1, v2;

			public Edge (int vertexIdx1, int vertexIdx2)
			{
				Debug.Assert (vertexIdx1 != vertexIdx2);

				// let's keep them sorted
				if (vertexIdx1 < vertexIdx2) {
					v1 = vertexIdx1;
					v2 = vertexIdx2;
				} else {
					v2 = vertexIdx1;
					v1 = vertexIdx2;
				}
			}

			public static Edge Get (int vertexIdx1, int vertexIdx2)
			{
				return new Edge (vertexIdx1, vertexIdx2);
			}

			public bool Equals (Edge e)
			{
				if (e.v1 == v1 && e.v2 == v2) {
					Debug.Assert (e.GetHashCode () == GetHashCode ());
					return true;
				} else {
					Debug.Assert (e.GetHashCode () != GetHashCode ());
					return false;
				}
			}

			public override bool Equals (object o)
			{
				return this.Equals (o as Edge);
			}

			public override int GetHashCode ()
			{
				return (v1 << 16) | (v2);  // should be okay, right?
			}
		}

		public static List<List<Edge>> GetBorderEdgeLoops (Mesh mesh)
		{
			var borderEdgeLoops = new List<List<Edge>> ();

			var edgesTriangles = new Dictionary<Edge, int> ();
			var adjacency = new List<int>[mesh.vertices.Length];

			var vertices = mesh.vertices;
			for (int i = 0; i < vertices.Length; ++i)
				adjacency [i] = new List<int> ();

			var triangles = mesh.triangles;
			for (int i = 0; i < triangles.Length; i += 3) {
				var a = triangles [i];
				var b = triangles [i + 1];
				var c = triangles [i + 2];

				adjacency [a].Add (b);
				adjacency [a].Add (c);

				adjacency [b].Add (a);
				adjacency [b].Add (c);

				adjacency [c].Add (a);
				adjacency [c].Add (b);

				var e1 = Edge.Get (a, b);
				var e2 = Edge.Get (b, c);
				var e3 = Edge.Get (c, a);

				int value;
				edgesTriangles [e1] = edgesTriangles.TryGetValue (e1, out value) ? value + 1 : 1;
				edgesTriangles [e2] = edgesTriangles.TryGetValue (e2, out value) ? value + 1 : 1;
				edgesTriangles [e3] = edgesTriangles.TryGetValue (e3, out value) ? value + 1 : 1;
			}

			var edgesVisited = new HashSet<Edge> ();
			foreach (var item in edgesTriangles) {
				var edge = item.Key;
				var numTrianglesForEdge = item.Value;
				if (numTrianglesForEdge <= 0 || numTrianglesForEdge > 2) {
					Debug.Assert (numTrianglesForEdge > 0 && numTrianglesForEdge <= 2);  // don't expect ill-formed meshes
				}

				// edges with one adjacent triangle are "border" edges
				if (numTrianglesForEdge != 1)
					continue;

				if (edgesVisited.Contains (edge))
					continue;

				// don't care what direction we go, let's choose v1
				int startingVertex = edge.v1;
				int currentEdgeLoopVertex = startingVertex;
				Edge currentEdge;

				List<Edge> border = new List<Edge> ();

				do {
					currentEdge = null;
					foreach (var adjacentVertex in adjacency[currentEdgeLoopVertex]) {
						var loopEdge = Edge.Get (currentEdgeLoopVertex, adjacentVertex);
						if (edgesVisited.Contains (loopEdge))
							continue;
						if (edgesTriangles [loopEdge] != 1)
							continue;

						currentEdge = loopEdge;
						currentEdgeLoopVertex = adjacentVertex;
						break;
					}

					if (currentEdge == null) {
						// could not find the next border edge
						break;
					}

					edgesVisited.Add (currentEdge);
					border.Add (currentEdge);
				} while (currentEdgeLoopVertex != startingVertex);

				borderEdgeLoops.Add (border);
			}

			return borderEdgeLoops;
		}

		public static TexturedMesh MergeMeshes(TexturedMesh mesh1, TexturedMesh mesh2)
		{
			TexturedMesh mergedMesh = new TexturedMesh();

			Vector3[] vertices1 = mesh1.mesh.vertices;
			Vector3[] vertices2 = mesh2.mesh.vertices;
			Vector3[] vertices = new Vector3[vertices1.Length + vertices2.Length];
			vertices1.CopyTo(vertices, 0);
			vertices2.CopyTo(vertices, vertices1.Length);

			int[] triangles1 = mesh1.mesh.triangles;
			int[] triangles2 = mesh2.mesh.triangles;
			int[] triangles = new int[triangles1.Length + triangles2.Length];
			triangles1.CopyTo(triangles, 0);
			triangles2.CopyTo(triangles, triangles1.Length);
			int idxOffset = vertices1.Length;
			for (int i = triangles1.Length; i < triangles.Length; i++)
				triangles[i] += idxOffset;

			Texture2D texture1 = mesh1.texture;
			Texture2D texture2 = mesh2.texture;
			Texture2D texture = new Texture2D(texture1.width + texture2.width, Math.Max(texture1.height, texture2.height));
			texture.SetPixels(0, 0, texture1.width, texture1.height, texture1.GetPixels());
			texture.SetPixels(texture1.width, 0, texture2.width, texture2.height, texture2.GetPixels());
			texture.Apply();

			Vector2[] uv1 = mesh1.mesh.uv;
			Vector2[] uv2 = mesh2.mesh.uv;
			Vector2[] uv = new Vector2[uv1.Length + uv2.Length];
			float xScale = ((float)texture1.width) / texture.width;
			float yScale = ((float)texture1.height) / texture.height;
			for (int i=0; i<uv1.Length; i++)
			{
				uv[i].x = uv1[i].x * xScale;
				uv[i].y = uv1[i].y * yScale;
			}

			float offset = xScale;
			xScale = ((float)texture2.width) / texture.width;
			yScale = ((float)texture2.height) / texture.height;
			for (int i=0; i<uv2.Length; i++)
			{
				uv[uv1.Length + i].x = offset + uv2[i].x * xScale;
				uv[uv1.Length + i].y = uv2[i].y * yScale;
			}

			mergedMesh.mesh = new Mesh()
			{
				vertices = vertices,
				triangles = triangles,
				uv = uv
			};
			mergedMesh.texture = texture;

			return mergedMesh;
		}
	}
}

