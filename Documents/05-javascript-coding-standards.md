# JavaScript Coding Standards & Clean Code Principles

## **Naming & Readability**
• **Use descriptive variable names** - `userAccount` instead of `ua`, `calculateTotalPrice` instead of `calc`
• **Use camelCase consistently** - `firstName`, `getUserData()`, `isLoggedIn`
• **Use verbs for functions, nouns for variables** - `getUser()` vs `user`, `validateEmail()` vs `emailValid`
• **Avoid abbreviations and mental mapping** - `temporaryUser` not `tempUsr`, readers shouldn't decode meanings

## **Functions & Methods**
• **Keep functions small and focused** - One responsibility per function, ideally under 20 lines
• **Use pure functions when possible** - Same input always produces same output, no side effects
• **Limit function parameters to 3 or fewer** - Use objects for multiple parameters: `createUser({name, email, age})`
• **Use early returns to reduce nesting** - `if (!isValid) return; // continue with main logic`

## **Variables & Constants**
• **Use const by default, let when reassignment needed** - Never use `var` in modern JavaScript
• **Declare variables close to their usage** - Minimize scope and improve readability
• **Use meaningful constants for magic numbers** - `const MAX_RETRY_ATTEMPTS = 3` instead of hardcoded `3`

## **Error Handling**
• **Use try-catch for async operations** - Always handle promise rejections and async errors
• **Fail fast with early validation** - Check inputs at function start, throw meaningful errors
• **Never use empty catch blocks** - Log errors or handle them explicitly

## **Code Structure**
• **Use consistent indentation (2 or 4 spaces)** - Pick one and stick to it project-wide
• **Group related functionality together** - Keep imports at top, group by feature/domain
• **Use modules for code organization** - Export/import instead of global variables

## **Modern JavaScript**
• **Use arrow functions for callbacks** - `array.map(item => item.name)` for concise syntax
• **Use destructuring for cleaner code** - `const {name, email} = user` instead of multiple assignments
• **Use template literals for string interpolation** - `Welcome ${name}!` instead of concatenation
• **Use async/await over promise chains** - More readable than `.then().catch()` chains

## **Performance & Efficiency**
• **Avoid deep object mutations** - Use spread operator or Object.assign for updates
• **Use strict equality (===) always** - Prevents unexpected type coercion bugs
• **Cache expensive operations** - Store results of heavy calculations or DOM queries
• **Use array methods appropriately** - `find()` for single items, `filter()` for subsets, `map()` for transformations

## **Code Quality**
• **Comment why, not what** - Explain business logic and complex decisions, not obvious code
• **Remove dead code immediately** - Don't leave commented-out code or unused functions
• **Use linting tools (ESLint)** - Enforce consistent style and catch common errors
• **Write self-documenting code first** - Good names reduce need for comments