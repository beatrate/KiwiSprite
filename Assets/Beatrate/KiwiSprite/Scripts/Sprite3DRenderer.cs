using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beatrate.KiwiSprite
{
	public enum Sprite3DBillboardMode
	{
		None = 0,
		Y = 1,
		XYZ = 2
	}

	[ExecuteAlways]
	[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
	public class Sprite3DRenderer : MonoBehaviour
	{
		public Sprite Sprite
		{
			get => sprite;
			set
			{
				if(sprite != value)
				{
					sprite = value;
					RebuildMesh();
					RebuildMaterial();

#if UNITY_EDITOR
					SpriteImportDetector.Unregister(this);
					SpriteImportDetector.Register(this);
#endif
				}
			}
		}
		[SerializeField]
		private Sprite sprite = null;

		public bool FlipX
		{
			get => flipX;
			set
			{
				if(flipX != value)
				{
					flipX = value;
					RebuildMaterial();
				}
			}
		}
		[SerializeField]
		private bool flipX = false;

		public bool FlipY
		{
			get => flipY;
			set
			{
				if(flipY != value)
				{
					flipY = value;
					RebuildMaterial();
				}
			}
		}
		[SerializeField]
		private bool flipY = false;

		public Sprite3DBillboardMode BillboardMode
		{
			get => billboardMode;
			set
			{
				if(billboardMode != value)
				{
					billboardMode = value;
					RebuildMaterial();
				}
			}
		}
		[SerializeField]
		private Sprite3DBillboardMode billboardMode = Sprite3DBillboardMode.None;

		public Material SharedMaterial
		{
			get => material;
			set
			{
				if(material != value)
				{
					material = value;
					RebuildMaterial();
				}
			}
		}

		[SerializeField]
		private Material material = null;

		public MeshRenderer MeshRenderer
		{
			get
			{
				if(meshRenderer == null)
				{
					meshRenderer = GetComponent<MeshRenderer>();
				}

				return meshRenderer;
			}
		}
		private MeshRenderer meshRenderer = null;

		public MeshFilter MeshFilter
		{
			get
			{
				if(meshFilter == null)
				{
					meshFilter = GetComponent<MeshFilter>();
				}

				return meshFilter;
			}
		}
		private MeshFilter meshFilter = null;

		private Material[] cachedMaterialArray = null;
		private Mesh mesh = null;
		private MaterialPropertyBlock propertyBlock = null;

		public void Awake()
		{
			meshRenderer = null;
			meshFilter = null;

			cachedMaterialArray = null;
			mesh = null;
			propertyBlock = new MaterialPropertyBlock();
		}

		public void OnEnable()
		{
			MeshRenderer.enabled = true;
			RebuildMesh();
			RebuildMaterial();

#if UNITY_EDITOR
			SpriteImportDetector.Register(this);
#endif
		}

		public void OnDisable()
		{
			MeshRenderer.enabled = false;
#if UNITY_EDITOR
			SpriteImportDetector.Unregister(this);
#endif
		}

		public void OnDestroy()
		{
			if(mesh != null)
			{
				SafeDestroy(mesh);
				mesh = null;
			}
		}

#if UNITY_EDITOR
		public void OnValidate()
		{
			if(!isActiveAndEnabled)
			{
				return;
			}	

			RebuildMesh();
			RebuildMaterial();
			SpriteImportDetector.Unregister(this);
			SpriteImportDetector.Register(this);
		}
#endif

		public void ForceRebuild()
		{
			RebuildMesh();
			RebuildMaterial();
		}

#if UNITY_EDITOR
		[MenuItem("GameObject/3D Object/Sprite 3D", false, 10)]
		private static void CreateSpriteRenderer(MenuCommand command)
		{
			GameObject go = new GameObject("Sprite 3D");
			GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
			go.AddComponent<Sprite3DRenderer>();
			Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
			Selection.activeObject = go;
		}
#endif

		private void SafeDestroy(UnityEngine.Object o)
		{
			if(Application.IsPlaying(gameObject))
			{
				Destroy(o);
			}
			else
			{
				DestroyImmediate(o);
			}
		}

		private void RebuildMesh()
		{
			if(cachedMaterialArray == null)
			{
				cachedMaterialArray = new Material[1];
			}

			cachedMaterialArray[0] = material;
			MeshRenderer.sharedMaterials = cachedMaterialArray;

			if(mesh == null)
			{
				mesh = new Mesh();
			}
			else
			{
				mesh.Clear();
			}

			if(MeshFilter.sharedMesh != mesh)
			{
				MeshFilter.sharedMesh = mesh;
			}

			if(sprite != null)
			{
				var verticesSlice = sprite.GetVertexAttribute<Vector3>(UnityEngine.Rendering.VertexAttribute.Position);
				var vertices = new NativeArray<Vector3>(verticesSlice.Length, Allocator.Temp);
				verticesSlice.CopyTo(vertices);

				var indices = sprite.GetIndices();
				var indicesCopy = new NativeArray<ushort>(indices.Length, Allocator.Temp);
				indices.CopyTo(indicesCopy);

				var uvsSlice = sprite.GetVertexAttribute<Vector2>(UnityEngine.Rendering.VertexAttribute.TexCoord0);
				var uvs = new NativeArray<Vector2>(uvsSlice.Length, Allocator.Temp);
				uvsSlice.CopyTo(uvs);

				mesh.subMeshCount = 1;
				mesh.SetVertices(vertices);
				mesh.SetIndices(indicesCopy, MeshTopology.Triangles, 0);
				mesh.SetUVs(0, uvs);
				mesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, indices.Length, MeshTopology.Triangles));
				mesh.RecalculateBounds();
				Bounds bounds = mesh.bounds;
				bounds.size = new Vector3(bounds.size.x, bounds.size.y, bounds.size.x);
				mesh.bounds = bounds;

				vertices.Dispose();
				indicesCopy.Dispose();
				uvs.Dispose();
			}
		}

		private void RebuildMaterial()
		{
			if(material == null)
			{
				return;
			}

			if(propertyBlock == null)
			{
				propertyBlock = new MaterialPropertyBlock();
			}

			Texture mainTexture = null;
			Texture alphaTexture = null;
			bool enableExternalAlpha = false;

			if(sprite != null)
			{
				mainTexture = sprite.texture;
				alphaTexture = sprite.associatedAlphaSplitTexture;
				enableExternalAlpha = alphaTexture != null;
			}

			if(mainTexture == null)
			{
				mainTexture = Texture2D.whiteTexture;
			}

			if(alphaTexture == null)
			{
				alphaTexture = Texture2D.whiteTexture;
			}

			if(mainTexture != null)
			{
				propertyBlock.SetTexture("_MainTex", mainTexture);
			}
			
			if(alphaTexture != null)
			{
				propertyBlock.SetTexture("_AlphaTex", alphaTexture);
			}
			
			propertyBlock.SetFloat("_EnableExternalAlpha", enableExternalAlpha ? 1.0f : 0.0f);
			Vector2 flip = new Vector2(flipX ? -1.0f : 1.0f, flipY ? -1.0f : 1.0f);
			propertyBlock.SetVector("_Flip", flip);

			propertyBlock.SetInteger("_BillboardMode", (int)billboardMode);

			MeshRenderer.SetPropertyBlock(propertyBlock);
		}
	}
}