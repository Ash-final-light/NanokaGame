using System;
using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    public sealed class KlotskiBoardLayout
    {
        private KlotskiBoardLayout(
            Vector2 boardTopLeft,
            int columns,
            int rows,
            float cellSize,
            float pieceGap,
            float boardPlaneZ)
        {
            BoardTopLeft = boardTopLeft;
            Columns = columns;
            Rows = rows;
            CellSize = cellSize;
            PieceGap = pieceGap;
            BoardPlaneZ = boardPlaneZ;

            BoardWorldSize = new Vector2(Columns * CellSize, Rows * CellSize);
            BoardBottomLeft = new Vector2(BoardTopLeft.x, BoardTopLeft.y - BoardWorldSize.y);
            BoardCenter = new Vector2(
                BoardTopLeft.x + BoardWorldSize.x * 0.5f,
                BoardTopLeft.y - BoardWorldSize.y * 0.5f);
        }

        public int Columns { get; }

        public int Rows { get; }

        public float CellSize { get; }

        public float PieceGap { get; }

        public float BoardPlaneZ { get; }

        public Vector2 BoardTopLeft { get; }

        public Vector2 BoardBottomLeft { get; }

        public Vector2 BoardCenter { get; }

        public Vector2 BoardWorldSize { get; }

        public static KlotskiBoardLayout CreateFromTopLeft(
            Vector2 boardTopLeft,
            int columns,
            int rows,
            float cellSize,
            float pieceGap,
            float boardPlaneZ)
        {
            ValidateParameters(boardTopLeft, columns, rows, cellSize, pieceGap, boardPlaneZ);

            return new KlotskiBoardLayout(
                boardTopLeft,
                columns,
                rows,
                cellSize,
                pieceGap,
                boardPlaneZ);
        }

        public static KlotskiBoardLayout CreateFromCenter(
            Vector2 boardCenter,
            int columns,
            int rows,
            float cellSize,
            float pieceGap,
            float boardPlaneZ)
        {
            ValidateParameters(boardCenter, columns, rows, cellSize, pieceGap, boardPlaneZ);

            Vector2 boardWorldSize = new Vector2(columns * cellSize, rows * cellSize);
            Vector2 boardTopLeft = new Vector2(
                boardCenter.x - boardWorldSize.x * 0.5f,
                boardCenter.y + boardWorldSize.y * 0.5f);

            return new KlotskiBoardLayout(
                boardTopLeft,
                columns,
                rows,
                cellSize,
                pieceGap,
                boardPlaneZ);
        }

        public static KlotskiBoardLayout CreateFromAnchorBounds(
            Bounds anchorWorldBounds,
            int columns,
            int rows,
            float cellSize,
            float pieceGap,
            float boardPlaneZ)
        {
            Vector2 boardTopLeft = new Vector2(anchorWorldBounds.min.x, anchorWorldBounds.max.y);

            return CreateFromTopLeft(
                boardTopLeft,
                columns,
                rows,
                cellSize,
                pieceGap,
                boardPlaneZ);
        }

        public Vector3 GetPieceWorldPosition(Vector2Int cell, Vector2Int sizeInCells)
        {
            ValidatePieceArea(cell, sizeInCells);

            float worldX = BoardBottomLeft.x + (cell.x + sizeInCells.x * 0.5f) * CellSize;
            float worldY = BoardBottomLeft.y + (cell.y + sizeInCells.y * 0.5f) * CellSize;

            return new Vector3(worldX, worldY, BoardPlaneZ);
        }

        public Vector2 GetPieceWorldSize(Vector2Int sizeInCells)
        {
            ValidatePieceSize(sizeInCells);

            return new Vector2(
                sizeInCells.x * CellSize - PieceGap,
                sizeInCells.y * CellSize - PieceGap);
        }

        public Vector2Int GetNearestCell(Vector3 worldPosition, Vector2Int sizeInCells)
        {
            ValidatePieceSize(sizeInCells);

            if (!IsFinite(worldPosition.x) || !IsFinite(worldPosition.y))
            {
                throw new ArgumentException("World position must contain finite X and Y values.", nameof(worldPosition));
            }

            int column = Mathf.RoundToInt(
                (worldPosition.x - BoardBottomLeft.x) / CellSize - sizeInCells.x * 0.5f);
            int row = Mathf.RoundToInt(
                (worldPosition.y - BoardBottomLeft.y) / CellSize - sizeInCells.y * 0.5f);

            return new Vector2Int(column, row);
        }

        public Bounds GetBoardWorldBounds()
        {
            return new Bounds(
                new Vector3(BoardCenter.x, BoardCenter.y, BoardPlaneZ),
                new Vector3(BoardWorldSize.x, BoardWorldSize.y, 0f));
        }

        private void ValidatePieceArea(Vector2Int cell, Vector2Int sizeInCells)
        {
            ValidatePieceSize(sizeInCells);

            if (cell.x < 0 ||
                cell.y < 0 ||
                cell.x + sizeInCells.x > Columns ||
                cell.y + sizeInCells.y > Rows)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(cell),
                    cell,
                    string.Format(
                        "Piece size {0} at cell {1} does not fit inside the {2}x{3} board.",
                        sizeInCells,
                        cell,
                        Columns,
                        Rows));
            }
        }

        private static void ValidatePieceSize(Vector2Int sizeInCells)
        {
            if (sizeInCells.x <= 0 || sizeInCells.y <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sizeInCells),
                    sizeInCells,
                    "Piece size must be positive on both axes.");
            }
        }

        private static void ValidateParameters(
            Vector2 anchor,
            int columns,
            int rows,
            float cellSize,
            float pieceGap,
            float boardPlaneZ)
        {
            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(columns), columns, "Columns must be greater than zero.");
            }

            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), rows, "Rows must be greater than zero.");
            }

            if (!IsFinite(cellSize) || cellSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSize), cellSize, "Cell size must be finite and greater than zero.");
            }

            if (!IsFinite(pieceGap) || pieceGap < 0f || pieceGap >= cellSize)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pieceGap),
                    pieceGap,
                    "Piece gap must be finite, non-negative, and smaller than cell size.");
            }

            if (!IsFinite(anchor.x) || !IsFinite(anchor.y))
            {
                throw new ArgumentException("Board anchor must contain finite X and Y values.", nameof(anchor));
            }

            if (!IsFinite(boardPlaneZ))
            {
                throw new ArgumentOutOfRangeException(nameof(boardPlaneZ), boardPlaneZ, "Board plane Z must be finite.");
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
