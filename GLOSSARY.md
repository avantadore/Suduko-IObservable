# Sudoku

Playing classic 9×9 Sudoku on a grid that propagates its own constraints: every placed digit causes its peers to eliminate that digit, and whatever the rules then force is placed automatically.

## Language

### The game and its state

**Game**:
A session in which one grid is filled in, one move at a time. Every game starts with all 81 cells empty.
_Avoid_: Session, match, board, puzzle

**Grid**:
The 81 cells and their current state within a game.
_Avoid_: Board, puzzle

**Cell**:
One of the 81 squares in a grid, identified by its row and column.
_Avoid_: Square, field, position

**Digit**:
One of the values 1–9 that a cell can hold.
_Avoid_: Number, value

**Candidate**:
A digit that is still possible for a cell. An empty cell in a new game has all nine; a filled cell has exactly one, its own digit.
_Avoid_: Option, possibility, value, pencil mark

### Filling cells

**Placement**:
A digit put in a cell, regardless of who put it there. A placement is final.
_Avoid_: Assignment, entry

**Move**:
A placement made by the player.
_Avoid_: Turn, input, guess

**Deduction**:
A placement the game made by itself because the rules forced it (a naked or hidden single).
_Avoid_: Auto-fill, inference, solver move

### Structure

**Unit**:
A row, column or box: nine cells that must together contain each digit exactly once.
_Avoid_: Group, house, region

**Box**:
One of the nine 3×3 units.
_Avoid_: Block, square, region

**Peer**:
Another cell that shares at least one unit with a cell. Every cell has 20 peers.
_Avoid_: Neighbour

### Propagation

**Elimination**:
Removing a candidate from a cell because one of its peers holds that digit.
_Avoid_: Pruning, crossing out

**Naked single**:
A cell with exactly one candidate left, which forces a deduction in that cell.
_Avoid_: Sole candidate, forced cell

**Hidden single**:
A digit that has only one possible cell within a unit, which forces a deduction there.
_Avoid_: Unique candidate

**Contradiction**:
A grid state that cannot lead to a solution: a cell with no candidates, or a digit that appears twice in a unit. A game in contradiction is stuck.
_Avoid_: Error, invalid state, conflict

**Step**:
One thing that happened to a grid: a move, a deduction, an elimination or a contradiction. A move is followed by the steps of its cascade, in order.
_Avoid_: Event, change, action

**Cascade**:
The chain of eliminations and deductions that one move sets off.
_Avoid_: Propagation run, chain reaction, ripple
