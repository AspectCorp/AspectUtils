// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import { siteLinks } from './src/site-links.ts';

// https://astro.build/config
export default defineConfig({
	site: 'https://aspectutils.pages.dev',
	integrations: [
		starlight({
			title: 'A$PECT UTILS',
			description: 'A$PECT UTILS — install apps, apply tweaks, run fixes, and manage Windows from one place.',
			logo: {
				src: './src/assets/branding/navlogo.png',
				replacesTitle: true,
			},
			favicon: '/favicon.svg',
			head: [
				{ tag: 'meta', attrs: { property: 'og:image', content: 'https://aspectutils.pages.dev/social-preview.png' } },
				{ tag: 'meta', attrs: { property: 'og:image:width', content: '1200' } },
				{ tag: 'meta', attrs: { property: 'og:image:height', content: '630' } },
				{ tag: 'meta', attrs: { name: 'twitter:image', content: 'https://aspectutils.pages.dev/social-preview.png' } },
			],
			social: [
				{ icon: 'github', label: 'GitHub', href: siteLinks.repo.href },
			],
			// Global theme overrides (colors, fonts, landing-page section styles).
			customCss: ['./src/styles/theme.css'],
			// Custom component overrides, all under src/components/.
			components: {
				ThemeProvider: './src/components/ThemeProvider.astro',
				Header: './src/components/Header.astro',
				Hero: './src/components/Hero.astro',
				Footer: './src/components/Footer.astro',
			},
			editLink: {
				baseUrl: 'https://github.com/AspectCorp/AspectUtils/edit/main/docs/',
			},
			// Sidebar groups, top to bottom: User Guide, Code Reference, Help.
			sidebar: [
				{
					label: 'User Guide',
					items: [
						{ label: 'Overview', slug: 'guides' },
						{ label: 'Getting Started', slug: 'guides/getting-started' },
						{ label: 'Applications', slug: 'guides/application' },
						{ label: 'Tweaks', slug: 'guides/tweaks' },
						{ label: 'Features', slug: 'guides/features' },
						{ label: 'Updates', slug: 'guides/updates' },
						{ label: 'Automation', slug: 'guides/automation' },
						{ label: 'Win11 Creator', slug: 'guides/win11creator' },
					],
				},
				{
					label: 'Code Reference',
					items: [
						{ label: 'Architecture & Design', slug: 'code-reference/architecture' },
						{ label: 'Issue Triage Commands', slug: 'code-reference/issue-triage' },
						{ label: 'Tweaks Reference', items: [{ autogenerate: { directory: 'code-reference/tweaks' } }] },
						{ label: 'Features Reference', items: [{ autogenerate: { directory: 'code-reference/features' } }] },
					],
				},
				{
					label: 'Help',
					items: [
						{ label: 'FAQ', slug: 'faq' },
						{ label: 'Known Issues', slug: 'knownissues' },
						{ label: 'Contributing', slug: 'contributing' },
						{ label: siteLinks.issues.label, link: siteLinks.issues.href, attrs: { target: '_blank', rel: 'noreferrer' } },
					],
				},
				{ label: siteLinks.repo.label, link: siteLinks.repo.href, attrs: { target: '_blank', rel: 'noreferrer' } },
			],
		}),
	],
});
