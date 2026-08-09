using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace NanokaGame.Games.Klotski
{
    public sealed class KlotskiBoardModel
    {
        private readonly KlotskiPieceState[,] _occupancy;
        private readonly List<KlotskiPieceState> _pieces;
        private readonly ReadOnlyCollection<KlotskiPieceState> _piecesView;
        private readonly Dictionary<string, KlotskiPieceState> _piecesById;
        private readonly KlotskiPieceState _targetPiece;

        public KlotskiBoardModel(KlotskiLevelDefinition level)
        {
            if (level == null)
            {
                throw new ArgumentNullException(nameof(level));
            }

            if (level.Columns <= 0 || level.Rows <= 0)
            {
                throw new ArgumentException("Board dimensions must be greater than zero.", nameof(level));
            }

            Columns = level.Columns;
            Rows = level.Rows;
            TargetCell = level.TargetCell;
            _occupancy = new KlotskiPieceState[Columns, Rows];
            _pieces = new List<KlotskiPieceState>(level.InitialPieces.Count);
            _piecesView = _pieces.AsReadOnly();
            _piecesById = new Dictionary<string, KlotskiPieceState>(StringComparer.Ordinal);

            KlotskiPieceState targetPiece = null;

            for (int pieceIndex = 0; pieceIndex < level.InitialPieces.Count; pieceIndex++)
            {
                KlotskiPieceState sourcePiece = level.InitialPieces[pieceIndex];
                ValidatePieceDefinition(sourcePiece, pieceIndex);

                if (_piecesById.ContainsKey(sourcePiece.Id))
                {
                    throw new ArgumentException(
                        string.Format("Piece ID '{0}' is duplicated.", sourcePiece.Id),
                        nameof(level));
                }

                KlotskiPieceState piece = sourcePiece.CreateInitialCopy();
                ValidatePlacementInsideBoard(piece.Id, piece.InitialCell, piece.SizeInCells, nameof(level));
                ValidateInitialPlacementIsFree(piece, nameof(level));

                _pieces.Add(piece);
                _piecesById.Add(piece.Id, piece);
                FillOccupancy(piece);

                if (piece.IsTarget)
                {
                    if (targetPiece != null)
                    {
                        throw new ArgumentException("A level must contain exactly one target piece.", nameof(level));
                    }

                    targetPiece = piece;
                }
            }

            if (targetPiece == null)
            {
                throw new ArgumentException("A level must contain exactly one target piece.", nameof(level));
            }

            ValidatePlacementInsideBoard(targetPiece.Id, TargetCell, targetPiece.SizeInCells, nameof(level));
            _targetPiece = targetPiece;
            IsCompleted = IsTargetAtGoal();
        }

        public int Columns { get; }

        public int Rows { get; }

        public Vector2Int TargetCell { get; }

        public bool IsCompleted { get; private set; }

        public IReadOnlyCollection<KlotskiPieceState> Pieces
        {
            get { return _piecesView; }
        }

        public bool TryGetPiece(string pieceId, out KlotskiPieceState piece)
        {
            if (pieceId == null)
            {
                piece = null;
                return false;
            }

            return _piecesById.TryGetValue(pieceId, out piece);
        }

        public string GetOccupantId(Vector2Int cell)
        {
            if (!IsCellInsideBoard(cell))
            {
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "Cell is outside the board.");
            }

            KlotskiPieceState occupant = _occupancy[cell.x, cell.y];
            return occupant == null ? null : occupant.Id;
        }

        public int GetMaxMoveDistance(string pieceId, Vector2Int direction)
        {
            KlotskiPieceState piece;
            if (IsCompleted || !TryGetPiece(pieceId, out piece) || !IsCardinalDirection(direction))
            {
                return 0;
            }

            int distance = 0;
            while (GetPlacementFailure(piece, piece.Cell + direction * (distance + 1)) == KlotskiMoveResult.Success)
            {
                distance++;
            }

            return distance;
        }

        public bool CanMove(string pieceId, Vector2Int direction, int distance)
        {
            return ValidateMove(pieceId, direction, distance) == KlotskiMoveResult.Success;
        }

        public KlotskiMoveResult TryMove(string pieceId, Vector2Int direction, int distance)
        {
            KlotskiMoveResult validationResult = ValidateMove(pieceId, direction, distance);
            if (validationResult != KlotskiMoveResult.Success)
            {
                return validationResult;
            }

            KlotskiPieceState piece = _piecesById[pieceId];
            ClearOccupancy(piece);
            piece.SetCell(piece.Cell + direction * distance);
            FillOccupancy(piece);
            IsCompleted = IsTargetAtGoal();

            return KlotskiMoveResult.Success;
        }

        public void Reset()
        {
            Array.Clear(_occupancy, 0, _occupancy.Length);

            for (int pieceIndex = 0; pieceIndex < _pieces.Count; pieceIndex++)
            {
                KlotskiPieceState piece = _pieces[pieceIndex];
                piece.ResetToInitialCell();
                FillOccupancy(piece);
            }

            IsCompleted = IsTargetAtGoal();
        }

        private KlotskiMoveResult ValidateMove(string pieceId, Vector2Int direction, int distance)
        {
            KlotskiPieceState piece;
            if (!TryGetPiece(pieceId, out piece))
            {
                return KlotskiMoveResult.PieceNotFound;
            }

            if (!IsCardinalDirection(direction))
            {
                return KlotskiMoveResult.InvalidDirection;
            }

            if (distance <= 0)
            {
                return KlotskiMoveResult.InvalidDistance;
            }

            if (IsCompleted)
            {
                return KlotskiMoveResult.GameAlreadyCompleted;
            }

            for (int step = 1; step <= distance; step++)
            {
                Vector2Int candidateCell = piece.Cell + direction * step;
                KlotskiMoveResult placementResult = GetPlacementFailure(piece, candidateCell);
                if (placementResult != KlotskiMoveResult.Success)
                {
                    return placementResult;
                }
            }

            return KlotskiMoveResult.Success;
        }

        private KlotskiMoveResult GetPlacementFailure(KlotskiPieceState piece, Vector2Int candidateCell)
        {
            if (!IsAreaInsideBoard(candidateCell, piece.SizeInCells))
            {
                return KlotskiMoveResult.OutOfBounds;
            }

            for (int offsetY = 0; offsetY < piece.SizeInCells.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < piece.SizeInCells.x; offsetX++)
                {
                    KlotskiPieceState occupant = _occupancy[candidateCell.x + offsetX, candidateCell.y + offsetY];
                    if (occupant != null && occupant != piece)
                    {
                        return KlotskiMoveResult.Blocked;
                    }
                }
            }

            return KlotskiMoveResult.Success;
        }

        private void ValidatePieceDefinition(KlotskiPieceState piece, int pieceIndex)
        {
            if (piece == null)
            {
                throw new ArgumentException(
                    string.Format("Initial piece at index {0} is null.", pieceIndex),
                    "level");
            }

            if (string.IsNullOrWhiteSpace(piece.Id))
            {
                throw new ArgumentException(
                    string.Format("Initial piece at index {0} has an empty ID.", pieceIndex),
                    "level");
            }

            if (piece.SizeInCells.x <= 0 || piece.SizeInCells.y <= 0)
            {
                throw new ArgumentException(
                    string.Format("Piece '{0}' must have a positive size.", piece.Id),
                    "level");
            }
        }

        private void ValidateInitialPlacementIsFree(KlotskiPieceState piece, string parameterName)
        {
            for (int offsetY = 0; offsetY < piece.SizeInCells.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < piece.SizeInCells.x; offsetX++)
                {
                    Vector2Int cell = piece.InitialCell + new Vector2Int(offsetX, offsetY);
                    KlotskiPieceState occupant = _occupancy[cell.x, cell.y];
                    if (occupant != null)
                    {
                        throw new ArgumentException(
                            string.Format(
                                "Piece '{0}' overlaps piece '{1}' at cell {2}.",
                                piece.Id,
                                occupant.Id,
                                cell),
                            parameterName);
                    }
                }
            }
        }

        private void ValidatePlacementInsideBoard(
            string pieceId,
            Vector2Int cell,
            Vector2Int sizeInCells,
            string parameterName)
        {
            if (!IsAreaInsideBoard(cell, sizeInCells))
            {
                throw new ArgumentException(
                    string.Format(
                        "Piece '{0}' with size {1} does not fit at cell {2} on a {3}x{4} board.",
                        pieceId,
                        sizeInCells,
                        cell,
                        Columns,
                        Rows),
                    parameterName);
            }
        }

        private bool IsCellInsideBoard(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
        }

        private bool IsAreaInsideBoard(Vector2Int cell, Vector2Int sizeInCells)
        {
            return cell.x >= 0 &&
                   cell.y >= 0 &&
                   cell.x + sizeInCells.x <= Columns &&
                   cell.y + sizeInCells.y <= Rows;
        }

        private static bool IsCardinalDirection(Vector2Int direction)
        {
            return direction == Vector2Int.left ||
                   direction == Vector2Int.right ||
                   direction == Vector2Int.up ||
                   direction == Vector2Int.down;
        }

        private void ClearOccupancy(KlotskiPieceState piece)
        {
            for (int offsetY = 0; offsetY < piece.SizeInCells.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < piece.SizeInCells.x; offsetX++)
                {
                    _occupancy[piece.Cell.x + offsetX, piece.Cell.y + offsetY] = null;
                }
            }
        }

        private void FillOccupancy(KlotskiPieceState piece)
        {
            for (int offsetY = 0; offsetY < piece.SizeInCells.y; offsetY++)
            {
                for (int offsetX = 0; offsetX < piece.SizeInCells.x; offsetX++)
                {
                    _occupancy[piece.Cell.x + offsetX, piece.Cell.y + offsetY] = piece;
                }
            }
        }

        private bool IsTargetAtGoal()
        {
            return _targetPiece.Cell == TargetCell;
        }
    }
}
