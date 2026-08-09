using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    public sealed class KlotskiPieceState
    {
        public KlotskiPieceState(
            string id,
            KlotskiPieceType type,
            Vector2Int initialCell,
            Vector2Int sizeInCells)
        {
            Id = id;
            Type = type;
            InitialCell = initialCell;
            Cell = initialCell;
            SizeInCells = sizeInCells;
        }

        public string Id { get; }

        public KlotskiPieceType Type { get; }

        public Vector2Int Cell { get; private set; }

        public Vector2Int InitialCell { get; }

        public Vector2Int SizeInCells { get; }

        public bool IsTarget
        {
            get { return Type == KlotskiPieceType.Target; }
        }

        internal KlotskiPieceState CreateInitialCopy()
        {
            return new KlotskiPieceState(Id, Type, InitialCell, SizeInCells);
        }

        internal void SetCell(Vector2Int cell)
        {
            Cell = cell;
        }

        internal void ResetToInitialCell()
        {
            Cell = InitialCell;
        }
    }
}
