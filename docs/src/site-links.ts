// Shared external links, so astro.config.mjs and Header.astro can't drift out of sync.
export const siteLinks = {
	repo: { label: 'GitHub', href: 'https://github.com/flickfpz/AspectUtils' },
	issues: { label: 'Issues', href: 'https://github.com/flickfpz/AspectUtils/issues' },
} as const;
