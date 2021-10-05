using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Beatrate.KiwiSprite
{
	public class SpriteImportDetector
	{
#if UNITY_EDITOR
		public class SpritePostProcessor : AssetPostprocessor
		{
			public class SpriteAssetInfo
			{
				public string AssetPath;
				public Texture2D Texture;
				public Sprite[] Sprites;
			}

			public static event Action<SpriteAssetInfo> ProcessedSprites;

			public void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
			{
				try
				{
					var info = new SpriteAssetInfo
					{
						AssetPath = assetPath,
						Texture = texture,
						Sprites = sprites
					};
					ProcessedSprites?.Invoke(info);
				}
				catch(Exception e)
				{
					Debug.LogException(e);
				}
			}
		}

		private class SpriteTracker
		{
			public struct SpriteId : IEquatable<SpriteId>
			{
				public string Guid { get; }
				public long LocalId { get; }

				public SpriteId(string guid, long localId)
				{
					Guid = guid;
					LocalId = localId;
				}

				public override bool Equals(object other)
				{
					return other is SpriteId spriteId && Equals(spriteId);
				}

				public override int GetHashCode()
				{
					return (Guid, LocalId).GetHashCode();
				}

				public bool Equals(SpriteId other)
				{
					return Guid == other.Guid && LocalId == other.LocalId;
				}

				public static bool operator ==(SpriteId l, SpriteId r)
				{
					return l.Equals(r);
				}

				public static bool operator !=(SpriteId l, SpriteId r)
				{
					return !(l == r);
				}
			}

			public Dictionary<int, Sprite3DRenderer> Renderers { get; } = new Dictionary<int, Sprite3DRenderer>();
			public Dictionary<SpriteId, List<int>> SpriteIdToRendererIdMap { get; } = new Dictionary<SpriteId, List<int>>();
			public Dictionary<int, SpriteId> RendererIdToSpriteIdMap { get; } = new Dictionary<int, SpriteId>();

			public Dictionary<SpriteId, string> SpriteIdToAssetPathMap { get; } = new Dictionary<SpriteId, string>();
			public Dictionary<string, List<SpriteId>> AssetPathToSpriteIdMap { get; } = new Dictionary<string, List<SpriteId>>();

			public void Clear()
			{
				Renderers.Clear();
				SpriteIdToRendererIdMap.Clear();
				RendererIdToSpriteIdMap.Clear();

				SpriteIdToAssetPathMap.Clear();
				AssetPathToSpriteIdMap.Clear();
			}

			public int GetRendererId(Sprite3DRenderer renderer)
			{
				return renderer.GetInstanceID();
			}

			public string GetAssetPath(Sprite sprite)
			{
				return AssetDatabase.GetAssetPath(sprite);
			}

			public bool TryGetSpriteId(Sprite sprite, out SpriteId spriteId)
			{
				if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out string guid, out long localId))
				{
					spriteId = new SpriteId(guid, localId);
					return true;
				}

				spriteId = default;
				return false;
			}

			public bool IsTracked(Sprite sprite)
			{
				if(TryGetSpriteId(sprite, out SpriteId spriteId))
				{
					return SpriteIdToRendererIdMap.ContainsKey(spriteId);
				}

				return false;
			}

			public bool IsTracked(SpriteId spriteId)
			{
				return SpriteIdToRendererIdMap.ContainsKey(spriteId);
			}

			public void Track(Sprite3DRenderer renderer)
			{
				if(renderer == null)
				{
					return;
				}

				if(renderer.Sprite == null)
				{
					return;
				}

				if(!TryGetSpriteId(renderer.Sprite, out SpriteId spriteId))
				{
					return;
				}

				int rendererId = GetRendererId(renderer);
				if(Renderers.ContainsKey(rendererId))
				{
					return;
				}

				Renderers.Add(rendererId, renderer);
				if(!SpriteIdToRendererIdMap.TryGetValue(spriteId, out List<int> rendererIdsWithThisSprite))
				{
					rendererIdsWithThisSprite = new List<int>();
					SpriteIdToRendererIdMap.Add(spriteId, rendererIdsWithThisSprite);
				}
				rendererIdsWithThisSprite.Add(rendererId);

				RendererIdToSpriteIdMap.Add(rendererId, spriteId);

				string assetPath = GetAssetPath(renderer.Sprite);
				if(!SpriteIdToAssetPathMap.ContainsKey(spriteId))
				{
					SpriteIdToAssetPathMap.Add(spriteId, assetPath);
					if(!AssetPathToSpriteIdMap.TryGetValue(assetPath, out List<SpriteId> spriteIdsWithThisPath))
					{
						spriteIdsWithThisPath = new List<SpriteId>();
						AssetPathToSpriteIdMap.Add(assetPath, spriteIdsWithThisPath);
					}

					spriteIdsWithThisPath.Add(spriteId);
				}
			}

			public void StopTracking(Sprite3DRenderer renderer)
			{
				if(renderer == null)
				{
					return;
				}

				int rendererId = GetRendererId(renderer);
				if(!Renderers.ContainsKey(rendererId))
				{
					return;
				}

				Renderers.Remove(rendererId);
				if(RendererIdToSpriteIdMap.TryGetValue(rendererId, out SpriteId spriteId))
				{
					RendererIdToSpriteIdMap.Remove(rendererId);
					SpriteIdToRendererIdMap.Remove(spriteId);

					if(SpriteIdToAssetPathMap.TryGetValue(spriteId, out string assetPath))
					{
						var spriteIdsWithThisPath = AssetPathToSpriteIdMap[assetPath];
						spriteIdsWithThisPath.Remove(spriteId);

						if(spriteIdsWithThisPath.Count == 0)
						{
							AssetPathToSpriteIdMap.Remove(assetPath);
							SpriteIdToAssetPathMap.Remove(spriteId);
						}
					}
				}
			}

			public IEnumerable<Sprite3DRenderer> GetTrackeddRenderersBySpriteId(SpriteId spriteId)
			{
				if(SpriteIdToRendererIdMap.TryGetValue(spriteId, out List<int> rendererIds))
				{
					foreach(int rendererId in rendererIds)
					{
						yield return Renderers[rendererId];
					}
				}
			}

			public IEnumerable<SpriteId> GetTrackedSpriteIdsAtPath(string assetPath)
			{
				if(AssetPathToSpriteIdMap.TryGetValue(assetPath, out List<SpriteId> spriteIds))
				{
					foreach(SpriteId spriteId in spriteIds)
					{
						yield return spriteId;
					}
				}
			}
		}

		private static SpriteTracker tracker = null;
		private static List<SpriteTracker.SpriteId> dirtySpriteIds = null;

		[InitializeOnLoadMethod]
		public static void Initialize()
		{
			SpritePostProcessor.ProcessedSprites -= OnProcessedSprites;
			SpritePostProcessor.ProcessedSprites += OnProcessedSprites;

			EditorApplication.playModeStateChanged += OnEditorPlayModeStateChanged;
		}

		private static void OnEditorPlayModeStateChanged(PlayModeStateChange change)
		{
			if(change == PlayModeStateChange.ExitingEditMode)
			{
				if(tracker != null)
				{
					tracker.Clear();
				}
			}
		}

		private static void OnProcessedSprites(SpritePostProcessor.SpriteAssetInfo info)
		{
			if(tracker == null)
			{
				return;
			}

			if(dirtySpriteIds == null)
			{
				dirtySpriteIds = new List<SpriteTracker.SpriteId>();
			}

			int countBefore = dirtySpriteIds.Count;
			dirtySpriteIds.AddRange(tracker.GetTrackedSpriteIdsAtPath(info.AssetPath));

			if(countBefore == 0 && dirtySpriteIds.Count > 0)
			{
				EditorApplication.delayCall += DelayedNotifyRenderers;
			}
		}

		private static void DelayedNotifyRenderers()
		{
			if(tracker == null)
			{
				return;
			}

			for(int i = 0; i < dirtySpriteIds.Count; ++i)
			{
				SpriteTracker.SpriteId spriteId = dirtySpriteIds[i];
				foreach(Sprite3DRenderer renderer in tracker.GetTrackeddRenderersBySpriteId(spriteId))
				{
					renderer.ForceRebuild();
				}
			}

			dirtySpriteIds.Clear();
		}

		public static void Register(Sprite3DRenderer renderer)
		{
			if(Application.isPlaying)
			{
				return;
			}

			EnsureTrackerCreated();
			tracker.Track(renderer);
		}

		public static void Unregister(Sprite3DRenderer renderer)
		{
			if(Application.isPlaying)
			{
				return;
			}

			EnsureTrackerCreated();
			tracker.StopTracking(renderer);
		}

		private static void EnsureTrackerCreated()
		{
			if(tracker == null)
			{
				tracker = new SpriteTracker();
			}
		}
#endif
	}

}