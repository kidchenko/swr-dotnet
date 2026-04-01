// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

import tailwindcss from '@tailwindcss/vite';

// https://astro.build/config
export default defineConfig({
  site: 'https://kidchenko.github.io',
  base: '/swr-dotnet',

  integrations: [
      starlight({
          title: 'SWR .NET',
          social: [{ icon: 'github', label: 'GitHub', href: 'https://github.com/kidchenko/swr-dotnet' }],
          customCss: ['./src/styles/global.css', './src/styles/starlight-custom.css'],
          sidebar: [
              {
                  label: 'Getting Started',
                  items: [
                      { label: 'Introduction', slug: 'getting-started/introduction' },
                  ],
              },
              {
                  label: 'Guides',
                  items: [
                      { label: 'Blazor Integration', slug: 'guides/blazor-integration' },
                      { label: 'ASP.NET Core Integration', slug: 'guides/aspnetcore-integration' },
                  ],
              },
              {
                  label: 'Reference',
                  autogenerate: { directory: 'reference' },
              },
          ],
      }),
	],

  vite: {
    plugins: [tailwindcss()],
  },
});