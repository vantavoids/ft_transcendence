import type { Metadata } from 'next';
import Link from 'next/link';
import { LegalPage } from '../../src/components/legal-page';

export const metadata: Metadata = {
  title: 'Terms of Service - ft_discord',
  description: 'Terms of Service for ft_discord, a student project built at 42.'
};

export default function TermsPage() {
  return (
    <LegalPage eyebrow="ft_discord - legal" title="Terms of Service" lastUpdated="7 July 2026">
      <h2>1. What this is</h2>
      <p>
        ft_discord (&ldquo;the Service&rdquo;) is a student project built to fulfill the
        ft_transcendence project of the 42 common core curriculum. It is a real-time
        communication platform (accounts, guilds/channels, direct messages, friends, notifications)
        built for educational purposes: to demonstrate microservice architecture, authentication,
        WebSocket handling, and related engineering practice.
      </p>
      <p>
        The Service is not a commercial product. It carries no uptime guarantee, no service-level
        agreement, and may be taken offline, reset, or have its data wiped without notice, including
        for grading, redeployment, or the end of the evaluation period. Do not rely on it to store
        anything you need to keep.
      </p>

      <h2>2. Eligibility</h2>
      <p>
        Under French and EU data protection law, a person may consent to the processing of their own
        personal data online from age 15. If you are under 15, you may not create an account. If you
        are between 15 and 18, you confirm you are legally permitted to use online services of this
        kind under the law that applies to you.
      </p>
      <p>
        The Service does not currently perform age verification. This is a statement of eligibility,
        not a representation that age is checked.
      </p>

      <h2>3. Accounts</h2>
      <p>
        You can create an account with an email and password, or via OAuth through GitHub, Google,
        or the 42 intranet. You&rsquo;re responsible for whatever happens under your account and for
        keeping your credentials confidential. Contact us if you think your account has been
        compromised.
      </p>
      <p>
        You may delete your account at any time. This is permanent. See the{' '}
        <Link href="/privacy">Privacy Policy</Link> for exactly what happens to your data when you
        do.
      </p>

      <h2>4. Acceptable use</h2>
      <p>You agree not to:</p>
      <ul>
        <li>Use the Service to harass, threaten, or abuse other users</li>
        <li>
          Post content that is illegal under French law, including content that infringes someone
          else&rsquo;s rights
        </li>
        <li>Attempt to bypass rate limiting, authentication, or permission checks</li>
        <li>
          Use the Service to distribute malware or attempt to compromise other users&rsquo; accounts
          or the underlying infrastructure
        </li>
        <li>Impersonate another person or entity</li>
      </ul>
      <p>
        Guild owners and members with the relevant role permissions can moderate their own guilds
        (kick, ban, manage roles, delete messages) using the tools built into the platform. We do
        not pre-screen content in guilds or DMs.
      </p>

      <h2>5. Content</h2>
      <p>
        You retain ownership of whatever you post (messages, avatars, banners, guild names and
        icons). By posting it, you grant the Service the license needed to store, transmit, and
        display it to the other users you&rsquo;ve shared it with (channel members, DM recipients,
        guild members), for as long as your account and that content exist.
      </p>
      <p>
        Deleting a message removes it from the interface for other users going forward. It does not
        undo the fact that other users may have already seen or copied it before you deleted it.
      </p>

      <h2>6. Termination</h2>
      <p>
        We may suspend or terminate accounts that violate Section 4, without prior notice,
        particularly where continued access poses a risk to other users or to the integrity of the
        Service.
      </p>
      <p>You may terminate your own account at any time via account deletion.</p>

      <h2>7. No warranty</h2>
      <p>
        This is a student project. The Service is provided &ldquo;as is,&rdquo; without warranty of
        any kind, express or implied, including fitness for a particular purpose, availability, or
        data durability. To the extent permitted by French law, the developers are not liable for
        any damages arising from use of, or inability to use, the Service.
      </p>

      <h2>8. Changes</h2>
      <p>
        These terms may change as the project evolves. Material changes will be reflected here with
        an updated date.
      </p>

      <h2>9. Governing law</h2>
      <p>
        These terms are governed by French law. Any dispute is subject to the exclusive jurisdiction
        of the competent French courts.
      </p>

      <h2>10. Contact</h2>
      <p>
        <a href="mailto:yandry@student.42.fr">yandry@student.42.fr</a>
      </p>

      <hr />
      <p>
        <em>
          See also: the <Link href="/privacy">Privacy Policy</Link> for how your data is collected,
          used, and how to exercise your rights under the GDPR.
        </em>
      </p>
    </LegalPage>
  );
}
