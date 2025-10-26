using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace run;

public static class Program
{
    private static readonly Dictionary<char, int> EnergyCost = new() 
    {
        { 'A', 1 }, { 'B', 10 }, { 'C', 100 }, { 'D', 1000 }
    };

    private static readonly Dictionary<char, int> RoomsIdx = new()
    {
        { 'A', 0 }, { 'B', 1 }, { 'C', 2 }, { 'D', 3 }
    };

    private static readonly List<int> RoomPos = [2, 4, 6, 8];
    private static readonly List<int> ValidHallwayStops = [0, 1, 3, 5, 7, 9, 10];

    
    public static void Main()
    {
        var lines = new List<string>();
            
        while (Console.ReadLine() is { } line && !string.IsNullOrWhiteSpace(line)) 
            lines.Add(line);
            
        var roomDepth = lines.Count - 3;
        var hallway = lines[1].Substring(1, 11);
        var rooms = new List<string> { "", "", "", "" };
            
        for (var d = 0; d < roomDepth; d++)
        {
            var roomLine = lines[2 + d];
            rooms[0] += roomLine[3];
            rooms[1] += roomLine[5];
            rooms[2] += roomLine[7];
            rooms[3] += roomLine[9];
        }

        var initialState = new State(hallway, rooms, roomDepth);

        var result = Solve(initialState);
        Console.WriteLine(result);
    }

    private static int Solve(State initialState)
    {
        var priorityQueue = new PriorityQueue<State, int>();
        var costs = new Dictionary<State, int> { [initialState] = 0 };

        priorityQueue.Enqueue(initialState, CalculateHeuristic(initialState));

        while (priorityQueue.Count > 0)
        {
            var curState = priorityQueue.Dequeue();
            var curCost = costs[curState];

            if (curState.IsFinal())
                return curCost;
                
            if (costs.TryGetValue(curState, out var knownCost) && curCost > knownCost)
                continue;
                
            foreach (var (nextState, moveCost) in GetNextStates(curState))
            {
                var newCost = curCost + moveCost;

                if (costs.ContainsKey(nextState) && newCost >= costs[nextState]) 
                    continue;
                costs[nextState] = newCost;
                priorityQueue.Enqueue(nextState, newCost + CalculateHeuristic(nextState));
            }
        }

        return -1;
    }

    private static int CalculateHeuristic(State state)
    {
        var heuristic = 0;
        for (var i = 0; i < state.Hallway.Length; i++)
        {
            var enemy = state.Hallway[i];
            if (enemy == '.') 
                continue;
            
            var pos = RoomPos[RoomsIdx[enemy]];
                
            var dist = Math.Abs(i - pos) + 1;
            heuristic += dist * EnergyCost[enemy];
        }
            
        for (var i = 0; i < 4; i++)
        {
            var room = state.Rooms[i];
            if (room.All(c => c == "ABCD"[i])) 
                continue;

            for (var depth = 0; depth < room.Length; depth++)
            {
                var enemy = room[depth];
                if (RoomsIdx[enemy] == i) continue;

                var stepsToExit = depth + 1;
                var target = RoomPos[RoomsIdx[enemy]];
                var dist = stepsToExit + Math.Abs(RoomPos[i] - target) + 1;
                heuristic += dist * EnergyCost[enemy];
            }
        }
        return heuristic;
    }

    private static IEnumerable<(State newState, int cost)> GetNextStates(State state)
    {
        for (var roomIdx = 0; roomIdx < 4; roomIdx++)
        {
            if (state.Rooms[roomIdx].Length == 0) continue;
                
            if (state.Rooms[roomIdx].All(c => c == "ABCD"[roomIdx])) 
                continue;
                
            var enemy = state.Rooms[roomIdx][0];
            var entryPos = RoomPos[roomIdx];

            foreach (var hallPos in ValidHallwayStops)
            {
                var (start, end) = (Math.Min(entryPos, hallPos), Math.Max(entryPos, hallPos));
                var isClear = true;
                for (var i = start; i <= end; i++)
                {
                    if (i == entryPos || state.Hallway[i] == '.') 
                        continue;
                    isClear = false;
                    break;
                }

                if (!isClear) continue;
                    
                var stepsToExit = state.RoomDepth - state.Rooms[roomIdx].Length + 1;
                var steps = Math.Abs(hallPos - entryPos);
                var moveCost = stepsToExit + steps * EnergyCost[enemy];
                        
                var hall = new StringBuilder(state.Hallway) { [hallPos] = enemy };
                var rooms = state.Rooms.ToList();
                rooms[roomIdx] = rooms[roomIdx][1..];

                yield return (new State(hall.ToString(), rooms, state.RoomDepth), moveCost);
            }
        }
            
        for (var hallIdx = 0; hallIdx < state.Hallway.Length; hallIdx++)
        {
            var enemy = state.Hallway[hallIdx];
            if (enemy == '.') 
                continue;

            var target = RoomsIdx[enemy];
                
            if (state.Rooms[target].Any(c => c != "ABCD"[target])) 
                continue;

            var entryPos = RoomPos[target];
                
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
                
            var steps = Math.Abs(hallIdx - entryPos);
            var enterSteps = state.RoomDepth - state.Rooms[target].Length;
            var moveCost = steps + enterSteps * EnergyCost[enemy];

            var hall = new StringBuilder(state.Hallway) { [hallIdx] = '.' };
            var rooms = state.Rooms.ToList();
            rooms[target] = enemy + rooms[target];

            yield return (new State(hall.ToString(), rooms, state.RoomDepth), moveCost);
        }
    }
}

internal record State(string Hallway, IReadOnlyList<string> Rooms, int RoomDepth)
{

    public bool IsFinal()
    {
        if (Hallway.Any(c => c != '.'))
            return false;

        for (var i = 0; i < 4; i++)
            if (Rooms[i].Length != RoomDepth || Rooms[i].Any(c => c != "ABCD"[i]))
                return false;
        return true;
    }
}