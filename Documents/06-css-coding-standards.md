**CSS Coding Standards & Clean Code Principles**

Naming & Organization:

Use BEM methodology for class names - .header__logo--active creates clear component relationships
Use semantic class names, not visual ones - .primary-button instead of .red-button
Use kebab-case for all CSS names - .user-profile, #main-header, --primary-color
Group related styles together - Keep component styles in same section or file

Selectors & Specificity:

Keep specificity low and consistent - Avoid !important and overly specific selectors
Use classes over IDs for styling - IDs are for JavaScript, classes for CSS
Avoid deep nesting (max 3 levels) - .header .nav .item is cleaner than deeper chains
Use direct child selectors when needed - .parent > .child prevents unintended matches

Properties & Values

Use shorthand properties wisely - margin: 10px 0 instead of separate declarations when appropriate
Use relative units for scalability - rem, em, %, vh/vw over fixed px when possible
Define custom properties for repeated values - --primary-color: #3498db for maintainable themes
Use consistent spacing units - Stick to multiples of 4px or 8px for visual harmony

Layout & Responsiveness

Use Flexbox and Grid appropriately - Flexbox for 1D layouts, Grid for 2D layouts
Mobile-first responsive design - Start with mobile styles, add desktop with min-width media queries
Use logical properties when supported - margin-inline instead of margin-left/right
Avoid fixed heights when possible - Let content determine height for better responsive behavior

Code Structure

Order properties logically - Position, display, box model, typography, visual effects
Use consistent indentation (2 spaces) - Maintain readability in nested rules
One declaration per line - color: blue; margin: 10px; each on separate lines
Group media queries with components - Keep responsive styles near base styles

Performance & Efficiency

Minimize CSS file size - Remove unused styles, combine similar rules
Use efficient selectors - Class selectors perform better than attribute or pseudo selectors
Avoid expensive properties - Minimize use of box-shadow, complex transform, heavy filter
Optimize images and use appropriate formats - WebP for photos, SVG for icons

Maintainability

Comment complex or non-obvious styles - Explain why, not what: /* Prevents text jumping during font load */
Use CSS variables for theming - Makes color schemes and spacing changes manageable
Remove dead CSS regularly - Use tools to identify and eliminate unused styles
Validate CSS syntax - Use linters and validators to catch errors early