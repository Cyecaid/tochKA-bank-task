using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class Program
{
    private static readonly Dictionary<char, int> EnergyCost = new()
    {
        { 'A', 1 }, { 'B', 10 }, { 'C', 100 }, { 'D', 1000 }
    };

    private static readonly Dictionary<char, int> RoomsIdx = new()
    {
        { 'A', 0 }, { 'B', 1 }, { 'C', 2 }, { 'D', 3 }
    };

    private static readonly List<int> RoomPos = new() { 2, 4, 6, 8 };
    private static readonly List<int> ValidHallwayStops = new() { 0, 1, 3, 5, 7, 9, 10 };
    private const string Enemies = "ABCD";
    
    static int Solve(List<string> lines)
    {
        var depth = lines.Count - 3;
        var hall = new string('.', 11);
        var rooms = new List<string> { "", "", "", "" };

        for (var i = lines.Count - 2; i > 1; i--)
        {
            var roomLine = lines[i];
            if (roomLine.Length > 3 && char.IsLetter(roomLine[3])) rooms[0] += roomLine[3];
            if (roomLine.Length > 5 && char.IsLetter(roomLine[5])) rooms[1] += roomLine[5];
            if (roomLine.Length > 7 && char.IsLetter(roomLine[7])) rooms[2] += roomLine[7];
            if (roomLine.Length > 9 && char.IsLetter(roomLine[9])) rooms[3] += roomLine[9];
        }

        var initialState = new State(hall, rooms, depth);

        var priorityQueue = new PriorityQueue<(State state, int cost), int>();
        var visited = new Dictionary<State, int> { [initialState] = 0 };

        priorityQueue.Enqueue((initialState, 0), CalculateHeuristic(initialState));

        while (priorityQueue.TryDequeue(out var item, out _))
        {
            var (curState, curCost) = item;

            if (visited[curState] < curCost)
                continue;

            if (curState.IsFinal())
                return curCost;

            foreach (var (nextState, moveCost) in GetNextStates(curState))
            {
                var newCost = curCost + moveCost;

                if (visited.ContainsKey(nextState) && newCost >= visited[nextState]) 
                    continue;
                visited[nextState] = newCost;
                priorityQueue.Enqueue((nextState, newCost), newCost + CalculateHeuristic(nextState));
            }
        }
        return -1;
    }

    private static int CalculateHeuristic(State state)
    {
        var total = 0;

        for (var i = 0; i < state.Hallway.Length; i++)
        {
            var enemy = state.Hallway[i];
            if (enemy == '.') continue;
            
            var targetPos = RoomPos[RoomsIdx[enemy]];
            total += Math.Abs(i - targetPos) * EnergyCost[enemy];
        }

        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        {
            var room = state.Rooms[roomIdx];
            var type = GetType(roomIdx);
            for (var depth = 0; depth < room.Length; depth++)
            {
                var enemy = room[depth];
                if (enemy == type && room[depth..].All(c => c == type))
                    continue;

                var stepsToExit = room.Length - depth;
                var targetPos = RoomPos[RoomsIdx[enemy]];
                var distance = Math.Abs(RoomPos[roomIdx] - targetPos);
                total += (stepsToExit + distance) * EnergyCost[enemy];
            }
        }
        return total;
    }

    private static IEnumerable<(State newState, int cost)> GetNextStates(State state)
    {
        for (var hallIdx = 0; hallIdx < state.Hallway.Length; hallIdx++)
        {
            var enemy = state.Hallway[hallIdx];
            if (enemy == '.') continue;

            var targetRoomIdx = RoomsIdx[enemy];
            if (!CheckEnterRoom(state.Rooms[targetRoomIdx], enemy)) continue;

            var entryPos = RoomPos[targetRoomIdx];
            var (start, end) = (Math.Min(hallIdx, entryPos), Math.Max(hallIdx, entryPos));

            var isClear = true;
            for (var i = start; i <= end; i++)
            {
                if (i == hallIdx || state.Hallway[i] == '.') 
                    continue;
                isClear = false;
                break;
            }
            if (!isClear) continue;

            var stepsInHall = Math.Abs(hallIdx - entryPos);
            var stepsInRoom = state.RoomDepth - state.Rooms[targetRoomIdx].Length;
            var moveCost = (stepsInHall + stepsInRoom) * EnergyCost[enemy];

            var hall = new StringBuilder(state.Hallway) { [hallIdx] = '.' };
            var rooms = state.Rooms.ToList();
            rooms[targetRoomIdx] += enemy;

            yield return (new State(hall.ToString(), rooms, state.RoomDepth), moveCost);
        }

        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        {
            var room = state.Rooms[roomIdx];
            if (room.Length == 0) continue;

            var type = GetType(roomIdx);
            if (room.All(c => c == type)) continue;

            var enemy = room.Last();
            var entryPos = RoomPos[roomIdx];

            foreach (var hallPos in ValidHallwayStops)
            {
                var (start, end) = (Math.Min(entryPos, hallPos), Math.Max(entryPos, hallPos));
                var isClear = true;
                for (var i = start; i <= end; i++)
                {
                    if (state.Hallway[i] == '.') 
                        continue;
                    isClear = false;
                    break;
                }
                if (!isClear) continue;

                var stepsInRoom = state.RoomDepth - room.Length + 1;
                var stepsInHall = Math.Abs(hallPos - entryPos);
                var moveCost = (stepsInRoom + stepsInHall) * EnergyCost[enemy];

                var hall = new StringBuilder(state.Hallway) { [hallPos] = enemy };
                var rooms = state.Rooms.ToList();
                rooms[roomIdx] = room[..^1];

                yield return (new State(hall.ToString(), rooms, state.RoomDepth), moveCost);
            }
        }
    }

    private static bool CheckEnterRoom(string room, char enemyType) => room.All(c => c == enemyType);

    private static char GetType(int idx) => Enemies[idx];
    
    static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            lines.Add(line);
        }

        var result = Solve(lines);
        Console.WriteLine(result);
    }
    
    private record State(string Hallway, IReadOnlyList<string> Rooms, int RoomDepth)
    {
        public bool IsFinal()
        {
            for (var i = 0; i < 4; i++)
            {
                var type = Enemies[i];
                if (Rooms[i].Length != RoomDepth || Rooms[i].Any(c => c != type))
                    return false;
            }
            return true;
        }
    }
}