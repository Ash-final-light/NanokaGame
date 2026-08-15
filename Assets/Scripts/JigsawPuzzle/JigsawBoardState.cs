using System;

namespace NanokaGame.Games.Jigsaw
{
    public sealed class JigsawBoardState
    {
        private readonly int[] _pieceIdBySlot;
        private readonly int[] _slotIndexByPieceId;

        public JigsawBoardState(int columns, int rows)
        {
            if (columns <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(columns),
                    columns,
                    "Column count must be greater than zero.");
            }

            if (rows <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    rows,
                    "Row count must be greater than zero.");
            }

            int pieceCount = checked(columns * rows);

            Columns = columns;
            Rows = rows;
            _pieceIdBySlot = new int[pieceCount];
            _slotIndexByPieceId = new int[pieceCount];
            ResetSolved();
        }

        public int Columns { get; }

        public int Rows { get; }

        public int PieceCount
        {
            get { return _pieceIdBySlot.Length; }
        }

        public bool IsCompleted
        {
            get
            {
                for (int slotIndex = 0; slotIndex < _pieceIdBySlot.Length; slotIndex++)
                {
                    if (_pieceIdBySlot[slotIndex] != slotIndex)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public int GetPieceIdAtSlot(int slotIndex)
        {
            ValidateSlotIndex(slotIndex, nameof(slotIndex));
            return _pieceIdBySlot[slotIndex];
        }

        public int GetSlotIndexForPiece(int pieceId)
        {
            ValidatePieceId(pieceId, nameof(pieceId));
            return _slotIndexByPieceId[pieceId];
        }

        public bool SwapSlots(int firstSlotIndex, int secondSlotIndex)
        {
            ValidateSlotIndex(firstSlotIndex, nameof(firstSlotIndex));
            ValidateSlotIndex(secondSlotIndex, nameof(secondSlotIndex));

            if (firstSlotIndex == secondSlotIndex)
            {
                return false;
            }

            int firstPieceId = _pieceIdBySlot[firstSlotIndex];
            int secondPieceId = _pieceIdBySlot[secondSlotIndex];

            _pieceIdBySlot[firstSlotIndex] = secondPieceId;
            _pieceIdBySlot[secondSlotIndex] = firstPieceId;
            _slotIndexByPieceId[firstPieceId] = secondSlotIndex;
            _slotIndexByPieceId[secondPieceId] = firstSlotIndex;

            return true;
        }

        public void Shuffle(Random random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (PieceCount < 2)
            {
                throw new InvalidOperationException("A puzzle needs at least two pieces to be shuffled.");
            }

            do
            {
                ResetSolved();
                ShuffleOnce(random);
            }
            while (IsCompleted);

            RebuildPieceSlotLookup();
        }

        public void ResetSolved()
        {
            for (int slotIndex = 0; slotIndex < _pieceIdBySlot.Length; slotIndex++)
            {
                _pieceIdBySlot[slotIndex] = slotIndex;
                _slotIndexByPieceId[slotIndex] = slotIndex;
            }
        }

        private void ShuffleOnce(Random random)
        {
            for (int slotIndex = _pieceIdBySlot.Length - 1; slotIndex > 0; slotIndex--)
            {
                int randomSlotIndex = random.Next(slotIndex + 1);
                int pieceId = _pieceIdBySlot[slotIndex];
                _pieceIdBySlot[slotIndex] = _pieceIdBySlot[randomSlotIndex];
                _pieceIdBySlot[randomSlotIndex] = pieceId;
            }
        }

        private void RebuildPieceSlotLookup()
        {
            for (int slotIndex = 0; slotIndex < _pieceIdBySlot.Length; slotIndex++)
            {
                int pieceId = _pieceIdBySlot[slotIndex];
                _slotIndexByPieceId[pieceId] = slotIndex;
            }
        }

        private void ValidateSlotIndex(int slotIndex, string parameterName)
        {
            if (slotIndex < 0 || slotIndex >= PieceCount)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    slotIndex,
                    "Slot index is outside the board.");
            }
        }

        private void ValidatePieceId(int pieceId, string parameterName)
        {
            if (pieceId < 0 || pieceId >= PieceCount)
            {
                throw new ArgumentOutOfRangeException(
                    parameterName,
                    pieceId,
                    "Piece ID is outside the board.");
            }
        }
    }
}
