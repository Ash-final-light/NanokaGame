using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    public sealed class KlotskiLevelDefinition
    {
        private readonly ReadOnlyCollection<KlotskiPieceState> _initialPieces;

        public KlotskiLevelDefinition(
            int columns,
            int rows,
            Vector2Int targetCell,
            IEnumerable<KlotskiPieceState> initialPieces)
        {
            if (initialPieces == null)
            {
                throw new ArgumentNullException(nameof(initialPieces));
            }

            Columns = columns;
            Rows = rows;
            TargetCell = targetCell;
            _initialPieces = new List<KlotskiPieceState>(initialPieces).AsReadOnly();
        }

        public int Columns { get; }

        public int Rows { get; }

        public Vector2Int TargetCell { get; }

        public IReadOnlyList<KlotskiPieceState> InitialPieces
        {
            get { return _initialPieces; }
        }

        public static KlotskiLevelDefinition CreateDefault()
        {
            KlotskiPieceState[] pieces =
            {
                new KlotskiPieceState("nanoka_head", KlotskiPieceType.Target, new Vector2Int(1, 3), new Vector2Int(2, 2)),
                new KlotskiPieceState("nanoka_body", KlotskiPieceType.Horizontal, new Vector2Int(1, 2), new Vector2Int(2, 1)),
                new KlotskiPieceState("nanoka_left_arm", KlotskiPieceType.Vertical, new Vector2Int(0, 3), new Vector2Int(1, 2)),
                new KlotskiPieceState("nanoka_right_arm", KlotskiPieceType.Vertical, new Vector2Int(3, 3), new Vector2Int(1, 2)),
                new KlotskiPieceState("nanoka_left_leg", KlotskiPieceType.Vertical, new Vector2Int(0, 1), new Vector2Int(1, 2)),
                new KlotskiPieceState("nanoka_right_leg", KlotskiPieceType.Vertical, new Vector2Int(3, 1), new Vector2Int(1, 2)),
                new KlotskiPieceState("nanoka_body_left", KlotskiPieceType.Soldier, new Vector2Int(1, 1), Vector2Int.one),
                new KlotskiPieceState("nanoka_body_right", KlotskiPieceType.Soldier, new Vector2Int(2, 1), Vector2Int.one),
                new KlotskiPieceState("nanoka_skirt_left", KlotskiPieceType.Soldier, new Vector2Int(0, 0), Vector2Int.one),
                new KlotskiPieceState("nanoka_skirt_right", KlotskiPieceType.Soldier, new Vector2Int(3, 0), Vector2Int.one)
            };

            return new KlotskiLevelDefinition(4, 5, new Vector2Int(1, 0), pieces);
        }
    }
}
