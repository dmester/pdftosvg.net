# PostScript evaluator

There are two evaluators:
* **Compiled intermediate representation** (`PostScriptCompiledExpression`) - Resolves static control flow and stack operations, folds constant expressions and eliminates expressions that do not contribute to an output.
* **Stack based interpreter** (`PostScriptExpression`) - Fallback if the PostScript program uses branching or stack modifications that cannot be resolved statically.

Implementation notes:
* Numbers and boolean values are stored internally as 64-bit floating-point numbers. Their PostScript types are tracked separately where required.
* NaN and infinity propagate through floating-point operations.
* At function output, NaN is converted to 0 and all results are clipped to the function Range.
* `cvi` converts NaN to 0 and saturates values outside the 32-bit integer range.
* The optimizer may reassociate additions, subtractions and multiplications, even though floating-point operations are not associative. This enables more aggressive constant folding.
* Multiplication by zero is eliminated when the remaining expression is not considered capable of producing a non-finite value.

Limitations:
* Maximum block depth is 100, including the outer function.
* Compiled execution allows 500 instructions per evaluation.
* Interpreted execution allows 5,000 instructions per evaluation.
* Stack is limited to 100 values.
